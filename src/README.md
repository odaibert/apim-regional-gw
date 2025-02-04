# Azure API Management Geographic Routing Demo

This Level 200 demonstration shows how to implement custom routing to Azure API Management regional gateways using Traffic Manager. The demo uses a Jupyter notebook to provide an interactive experience for understanding and testing geographic-based routing.

## Prerequisites

1. Azure Subscription
2. Azure CLI installed and authenticated
3. Python environment with required packages:
   ```bash
   pip install jupyter matplotlib ipywidgets azure-identity azure-mgmt-trafficmanager
   ```

## Setup

1. Clone this repository
2. Set your Azure subscription ID:
   ```bash
   export AZURE_SUBSCRIPTION_ID="your-subscription-id"
   ```
3. Update the `RESOURCE_GROUP` variable in the notebook with your resource group name

## Running the Demo

1. Start Jupyter Notebook:
   ```bash
   jupyter notebook
   ```
2. Open `custom_routing_demo.ipynb`
3. Follow the interactive cells to:
   - Create a Traffic Manager profile
   - Configure regional endpoints
   - Test routing behavior with the interactive visualization

## What You'll Learn

- How to configure Traffic Manager for geographic routing
- Setting up API Management regional gateways as endpoints
- Testing and visualizing routing behavior
- Understanding how Traffic Manager routes requests based on geographic location

## Architecture

```
Client Request → Traffic Manager → Regional API Management Gateway
```

The demo shows how Traffic Manager routes requests to the nearest API Management gateway based on the client's geographic location, improving latency and availability.
