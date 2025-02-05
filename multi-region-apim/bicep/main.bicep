param location string = resourceGroup().location
param apimName string = 'apim-${uniqueString(resourceGroup().id)}'
param publisherName string = 'Contoso'
param publisherEmail string = 'admin@contoso.com'
param sku string = 'Premium'
param skuCount int = 1
param secondaryLocation string = 'westeurope'

// Mock backend configuration
param mockBackendUrl string = 'https://mockbin.org/bin/11f8b08b-1b6b-4f9d-a872-93c7e0efde2f'

resource apimService 'Microsoft.ApiManagement/service@2023-03-01-preview' = {
  name: apimName
  location: location
  sku: {
    name: sku
    capacity: skuCount
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    additionalLocations: [
      {
        location: secondaryLocation
        sku: {
          name: sku
          capacity: skuCount
        }
      }
    ]
  }
}

output apimName string = apimService.name
output primaryGatewayUrl string = apimService.properties.gatewayUrl
output secondaryGatewayUrl string = '${apimService.properties.gatewayUrl}'.replace(location, secondaryLocation)

// Mock backend API and operations
resource mockBackendApi 'Microsoft.ApiManagement/service/apis@2023-03-01-preview' = {
  parent: apimService
  name: 'mock-backend'
  properties: {
    displayName: 'Mock Backend API'
    path: 'mock'
    protocols: ['https']
    serviceUrl: mockBackendUrl
    subscriptionRequired: false
  }
}

resource mockBackendOperation 'Microsoft.ApiManagement/service/apis/operations@2023-03-01-preview' = {
  parent: mockBackendApi
  name: 'mock-operation'
  properties: {
    displayName: 'Mock Operation'
    method: 'GET'
    urlTemplate: '/'
    responses: [
      {
        statusCode: 200
        description: 'Successful response'
      }
    ]
  }
}
