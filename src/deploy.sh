#!/bin/bash

# Build and deploy the API to multiple regions
RESOURCE_GROUP="your-resource-group"
REGIONS=("eastus" "westeurope")
ACR_NAME="apimdemodemo"

az acr create --resource-group $RESOURCE_GROUP --name $ACR_NAME --sku Basic
az acr build --registry $ACR_NAME --image apim-demo:latest ./api

for region in "${REGIONS[@]}"; do
    az appservice plan create \
        --name "plan-$region" \
        --resource-group $RESOURCE_GROUP \
        --location $region \
        --sku B1 \
        --is-linux

    az webapp create \
        --resource-group $RESOURCE_GROUP \
        --plan "plan-$region" \
        --name "apim-demo-$region" \
        --deployment-container-image-name "$ACR_NAME.azurecr.io/apim-demo:latest"

    az webapp config appsettings set \
        --resource-group $RESOURCE_GROUP \
        --name "apim-demo-$region" \
        --settings AZURE_REGION=$region
done
