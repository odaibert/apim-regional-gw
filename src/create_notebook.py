import nbformat as nbf

# Create a new notebook
nb = nbf.v4.new_notebook()

# Create markdown and code cells
cells = [
    nbf.v4.new_markdown_cell('''# Level 200: Geographic Routing with Azure API Management
This interactive notebook demonstrates how to implement geographic-based routing using Azure Traffic Manager with API Management regional gateways. You'll learn how to route API requests to the nearest regional gateway based on the client's geographic location.

## What You'll Learn
- Configure Traffic Manager with Geographic routing method
- Deploy regional API endpoints using Azure App Service
- Set up API Management gateways in multiple regions
- Configure Traffic Manager endpoints with geographic mapping
- Test and visualize routing behavior using real Azure IP ranges

## Prerequisites
- Azure subscription
- Azure CLI authenticated
- Resource group with permissions to create:
  - Traffic Manager profiles
  - API Management services
  - App Service plans and Web Apps
  - Azure Container Registry

## Architecture
```
Client Request (IP-based location)
    ↓
Traffic Manager (Geographic Routing)
    ↓
Regional API Management Gateway
    ↓
Regional Web App (FastAPI Backend)
```

## Setup Process
1. Deploy the API to multiple regions using Azure App Service
2. Create API Management gateways in corresponding regions
3. Configure Traffic Manager with Geographic routing
4. Add regional endpoints with geographic mapping
5. Test routing using simulated client locations'''),
    
    nbf.v4.new_code_cell('''import os
import json
import requests
import matplotlib.pyplot as plt
from IPython.display import display, HTML
import ipywidgets as widgets
from azure.identity import DefaultAzureCredential
from azure.mgmt.trafficmanager import TrafficManagerManagementClient'''),
    
    nbf.v4.new_markdown_cell('''## 1. Deploy Regional APIs
First, we'll deploy our FastAPI application to multiple regions using Azure App Service. The deployment script (`deploy.sh`) will:
1. Create an Azure Container Registry (ACR)
2. Build and push our Docker image
3. Create App Service plans in each region
4. Deploy the web app to each region
5. Configure environment variables

Run the deployment script:
```bash
chmod +x deploy.sh
./deploy.sh
```

## 2. Set Up Traffic Manager Profile
Now we'll create a Traffic Manager profile that will route requests based on the user's geographic location. This profile will use the Geographic routing method to direct users to the nearest regional endpoint.'''),
    
    nbf.v4.new_code_cell('''# Configuration
RESOURCE_GROUP = "your-resource-group"
TRAFFIC_MANAGER_NAME = "apim-geo-demo"
SUBSCRIPTION_ID = os.getenv("AZURE_SUBSCRIPTION_ID")

def create_traffic_manager():
    """Create Traffic Manager profile with Geographic routing"""
    cmd = f"""az network traffic-manager profile create \\
        --name {TRAFFIC_MANAGER_NAME} \\
        --resource-group {RESOURCE_GROUP} \\
        --routing-method Geographic \\
        --unique-dns-name {TRAFFIC_MANAGER_NAME} \\
        --monitor-protocol HTTPS \\
        --monitor-port 443 \\
        --monitor-path "/status-0123456789abcdef"
    """
    !{cmd}
    return f"{TRAFFIC_MANAGER_NAME}.trafficmanager.net"

traffic_manager_endpoint = create_traffic_manager()
print(f"Traffic Manager endpoint: {traffic_manager_endpoint}")'''),
    
    nbf.v4.new_markdown_cell('''## 3. Configure Regional Endpoints
Add the deployed web apps as endpoints in Traffic Manager with geographic mapping.'''),
    
    nbf.v4.new_code_cell('''def add_regional_endpoints():
    # Get Web App IDs
    eastus_id = !az webapp show --name apim-demo-eastus --resource-group {RESOURCE_GROUP} --query id -o tsv
    westeu_id = !az webapp show --name apim-demo-westeurope --resource-group {RESOURCE_GROUP} --query id -o tsv
    
    # Add East US endpoint
    !az network traffic-manager endpoint create \\
        --name eastus \\
        --profile-name {TRAFFIC_MANAGER_NAME} \\
        --resource-group {RESOURCE_GROUP} \\
        --type azureEndpoints \\
        --target-resource-id {eastus_id[0]} \\
        --geo-mapping GEO-NA \\
        --endpoint-status Enabled

    # Add West Europe endpoint
    !az network traffic-manager endpoint create \\
        --name westeurope \\
        --profile-name {TRAFFIC_MANAGER_NAME} \\
        --resource-group {RESOURCE_GROUP} \\
        --type azureEndpoints \\
        --target-resource-id {westeu_id[0]} \\
        --geo-mapping GEO-EU \\
        --endpoint-status Enabled

add_regional_endpoints()'''),
    
    nbf.v4.new_markdown_cell('''## 4. Test and Visualize Routing
Simulate requests from different geographic locations and visualize how Traffic Manager routes them.'''),
    
    nbf.v4.new_code_cell('''def create_test_interface():
    location_dropdown = widgets.Dropdown(
        options=['North America', 'Europe', 'Asia'],
        description='Location:',
        style={'description_width': 'initial'}
    )
    
    test_button = widgets.Button(description='Test Route')
    output = widgets.Output()
    
    def on_test_click(b):
        with output:
            output.clear_output()
            location = location_dropdown.value
            
            # Use real Azure IP ranges for testing
            test_ips = {
                'North America': '23.96.0.0',  # Azure East US
                'Europe': '13.80.0.0',         # Azure West Europe
                'Asia': '13.75.0.0'            # Azure Southeast Asia
            }
            test_ip = test_ips[location]
            
            try:
                response = requests.get(
                    f'https://{traffic_manager_endpoint}',
                    headers={'X-Client-IP': test_ip},
                    timeout=10
                )
                data = response.json()
                region = data['region']
                
                plt.figure(figsize=(10, 6))
                plt.plot(['Client', 'Traffic Manager', 'API Gateway'], [1, 2, 1], 'bo-')
                plt.title(f'Request Route: {location} → {region}')
                plt.grid(True)
                plt.show()
                
                print(f'Request from: {location} (IP: {test_ip})')
                print(f'Routed to: {region}')
                print(f'Response: {data["message"]}')
            except Exception as e:
                print(f"Error making request: {str(e)}")
                print(f"Traffic Manager URL: {traffic_manager_endpoint}")
    
    test_button.on_click(on_test_click)
    display(location_dropdown, test_button, output)

# Create interactive test interface
create_test_interface()''')
]

nb.cells = cells

# Write the notebook to a file
with open('custom_routing_demo.ipynb', 'w') as f:
    nbf.write(nb, f)
