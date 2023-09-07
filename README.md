So far, this project uses Azure Storage Account, Azure Functions and Azure OpenAI to put transcript data in an Azure cognitive Search Service.

Whats right now missing in this repository is the prompt flow which is brings together azure search and azure openai. This will later be added. The promptflow endpoint deployment will than provide the "ML"-settings.

# deploy azure resource

to deploy the azure resources use [deploy.ps1](https://github.com/nampacx/MeetingNotes/blob/main/eng/deploy.ps1)

```
az login
az account set --s subscriptionId
.\deploy.ps1
```

# To run local

Get the necessary data from the azure resources and add them to the either local.setting.json or into local project secrets.

## Project secrets

recommended using secrets: [.net secrets extension](https://marketplace.visualstudio.com/items?itemName=adrianwilczynski.user-secrets)
and the following secrets

```
{
    "AzureWebJobsStorage": "<Storage account connection string>",
    "AZURE_SEARCH_ENDPOINT":"<azure search endpoint>",
    "AZURE_SEARCH_KEY":"<azure search key>",
    "OPENAI_KEY":"<open ai key>",
    "OPENAI_ENDPOINT":"<open ai endpoint>",
    "ML_MODEL_NAME": "<Azure ML model>",
    "ML_AUTHORIZATION_KEY": "<model endpoint key>",
    "ML_ENDPOINT": "<model endpoint>"
}
```

## Local setting

```
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "AzureWebJobsStorage": "<Storage account connection string>",
    "AZURE_SEARCH_ENDPOINT":"<azure search endpoint>",
    "AZURE_SEARCH_KEY":"<azure search key>",
    "OPENAI_KEY":"<open ai key>",
    "OPENAI_ENDPOINT":"<open ai endpoint>",
    "ML_MODEL_NAME": "<Azure ML model>",
    "ML_AUTHORIZATION_KEY": "<model endpoint key>",
    "ML_ENDPOINT": "<model endpoint>"
  }
}
```
