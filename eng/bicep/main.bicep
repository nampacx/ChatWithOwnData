@description('The name of the function app that you wish to create.')
param appName string = 'fnapp${uniqueString(resourceGroup().id)}'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'

@description('The language worker runtime to load in the function app.')
@allowed([
  'node'
  'dotnet'
  'java'
])
param functionWorkerRuntime string = 'dotnet'

@description('Specifies the Azure Active Directory tenant ID that should be used for authenticating requests to the key vault. Get it by using Get-AzSubscription cmdlet.')
param tenantId string = subscription().tenantId

@description('Specifies the permissions to secrets in the vault. Valid values are: all, get, list, set, delete, backup, restore, recover, and purge.')
param secretsPermissions array = [
  'get'
]

@description('Specifies whether the key vault is a standard vault or a premium vault.')
@allowed([
  'standard'
  'premium'
])
param keyVaultSkuName string = 'standard'

@description('Partitions allow for scaling of document count as well as faster indexing by sharding your index over multiple search units.')
@allowed([
  1
  2
  3
  4
  6
  12
])
param azureSearchPartitionCount int = 1

@allowed([
  'free'
  'basic'
  'standard'
  'standard2'
  'standard3'
  'storage_optimized_l1'
  'storage_optimized_l2'
])
@description('The pricing tier of the search service you want to create (for example, basic or standard).')
param azureSearchSku string = 'standard'

@description('Replicas distribute search workloads across the service. You need at least two replicas to support high availability of query workloads (not applicable to the free tier).')
@minValue(1)
@maxValue(12)
param azureSearchReplicaCount int = 1

@description('Applicable only for SKUs set to standard3. You can set this property to enable a single, high density partition that allows up to 1000 indexes, which is much higher than the maximum indexes allowed for any other SKU.')
@allowed([
  'default'
  'highDensity'
])
param azureSearchHostingMode string = 'default'

param openAiSku object = {
  name: 'S0'
}

var storageAccountName = '${uniqueString(resourceGroup().id)}azfunctions'
var functionAppName = appName
var hostingPlanName = appName
var searchName = appName
var openAiName = appName
var applicationInsightsName = appName
var keyVaultName = '${uniqueString(resourceGroup().id)}kv'
var transciriptionBlobContainerName = 'transcripts'
var searchIndexName = 'transcripts'

// Storage account

resource storageAccount 'Microsoft.Storage/storageAccounts@2022-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource transcriptBlobContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2022-05-01' = {
  name: '${storageAccount.name}/default/${transciriptionBlobContainerName}'
}

//Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Request_Source: 'rest'
  }
}

//KeyVault
resource kv 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    tenantId: tenantId
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    accessPolicies: [
      {
        objectId: functionApp.identity.principalId
        tenantId: tenantId
        permissions: {
          secrets: secretsPermissions
        }
      }
    ]
    sku: {
      name: keyVaultSkuName
      family: 'A'
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

resource stroageAccountConnectionString 'Microsoft.KeyVault/vaults/secrets@2021-11-01-preview' = {
  parent: kv
  name: 'AzureWebJobsStorage'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
  }
}

//OpenAI
var deployments = [
  {
    name: 'chat'
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
    sku: {
      name: 'Standard'
      capacity: 30
    }
  }
  {
    name: 'embedding'
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
    capacity: 30
  }
]

resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiName
  location: 'eastus'
  kind: 'OpenAI'
  sku: openAiSku
  properties:{
    publicNetworkAccess: 'Enabled'
  }
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
  parent: account
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
  }
  sku: contains(deployment, 'sku') ? deployment.sku : {
    name: 'Standard'
    capacity: 20
  }
}]

//Search Service
resource search 'Microsoft.Search/searchServices@2020-08-01' = {
  name: searchName
  location: location
  sku: {
    name: azureSearchSku
  }
  properties: {
    replicaCount: azureSearchReplicaCount
    partitionCount: azureSearchPartitionCount
    hostingMode: azureSearchHostingMode
  }
}

// Function App
resource hostingPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {}
}

resource functionApp 'Microsoft.Web/sites@2021-03-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      ftpsState: 'FtpsOnly'
      minTlsVersion: '1.2'
    }
    httpsOnly: true
  }
}

resource functionAppSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'appsettings'
  kind: 'string'
  parent: functionApp
  properties: {
    AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/${stroageAccountConnectionString.name}/)'
    WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/${stroageAccountConnectionString.name}/)'
    WEBSITE_CONTENTSHARE: toLower(functionAppName)
    FUNCTIONS_EXTENSION_VERSION: '~4'
    APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.properties.InstrumentationKey
    FUNCTIONS_WORKER_RUNTIME: functionWorkerRuntime
    OPENAI_ENDPOINT: account.properties.endpoint
    OPENAI_KEY: ''
    AZURE_SEARCH_INDEX: searchIndexName
    AZURE_SEARCH_ENDPOINT: 'https://${search.name}.search.windows.net/'
  }
}
