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
output secondaryGatewayUrl string = replace(string(apimService.properties.gatewayUrl), location, secondaryLocation)

// Traffic Manager configuration
resource trafficManagerProfile 'Microsoft.Network/trafficManagerProfiles@2023-05-01' = {
  name: '${apimName}-tm'
  location: 'global'
  properties: {
    profileStatus: 'Enabled'
    trafficRoutingMethod: 'Geographic'
    dnsConfig: {
      relativeName: '${apimName}-tm'
      ttl: 30
    }
    monitorConfig: {
      protocol: 'HTTPS'
      port: 443
      path: '/status-0123456789abcdef'
      intervalInSeconds: 30
      timeoutInSeconds: 10
      toleratedNumberOfFailures: 3
    }
    endpoints: [
      {
        name: 'primary-endpoint'
        type: 'Microsoft.Network/trafficManagerProfiles/azureEndpoints'
        properties: {
          targetResourceId: apimService.id
          endpointStatus: 'Enabled'
          endpointLocation: location
          geoMapping: ['WORLD']
        }
      }
      {
        name: 'secondary-endpoint'
        type: 'Microsoft.Network/trafficManagerProfiles/azureEndpoints'
        properties: {
          targetResourceId: apimService.id
          endpointStatus: 'Enabled'
          endpointLocation: secondaryLocation
          geoMapping: ['WORLD']
        }
      }
    ]
  }
}

output trafficManagerUrl string = 'https://${trafficManagerProfile.properties.dnsConfig.relativeName}.trafficmanager.net'

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
