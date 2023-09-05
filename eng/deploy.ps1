$rgName = "rg-meetingnotes"
$rgLocation = "westeurope"

az group create --name $rgName --location $rgLocation
az deployment group create --resource-group $rgName --template-file ./bicep/main.bicep