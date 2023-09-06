So far, this porject uses Azure Storage Account, Azure Functions and Azure OpenAI to put trnascript data in an Azure Cognitve Search Service.

Currently the workflow looks like this:

```mermaid
graph TD;
    A|Storage Account|-->|Blob trigger| B|TranscriptUploadedBlobTrigger|
    B-->|Extract index name (= blob file directory name)| C{Check if index exists}
    C-->|Yes| D
    C-->|No| E|Create new search index|
```


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
recommended using secrets:  [.net secrets extension](https://marketplace.visualstudio.com/items?itemName=adrianwilczynski.user-secrets)
and the following secrets
```
{
    "AzureWebJobsStorage": "<Storage account connection string>",
    "AZURE_SEARCH_ENDPOINT":"<azure search endpoint>",
    "AZURE_SEARCH_KEY":"<azure search key>",
    "OPENAI_KEY":"<open ai key>",
    "OPENAI_ENDPOINT":"<open ai endpoint>"
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
    "OPENAI_ENDPOINT":"<open ai endpoint>"
  }
}
```



