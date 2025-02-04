# Azure API Management Geographic Routing Demo

Level 200 demonstration of custom routing to Azure API Management regional gateways using Traffic Manager. This demo provides an interactive Jupyter notebook that showcases geographic-based routing principles.

## Features

- Interactive Jupyter notebook for hands-on learning
- Multi-region API deployment automation
- Traffic Manager configuration with geographic routing
- Visual request routing demonstration
- Real Azure IP range testing

## Prerequisites

- Azure Subscription
- Azure CLI installed and authenticated
- Python environment with Jupyter
- Docker for local testing

## Getting Started

1. Clone this repository
2. Navigate to the `src` directory
3. Follow the instructions in `custom_routing_demo.ipynb`

## Repository Structure

```
src/
├── custom_routing_demo.ipynb    # Main interactive demo
├── deploy.sh                    # Multi-region deployment script
└── api/                        # Backend implementation
    ├── main.py                 # FastAPI application
    ├── requirements.txt        # Python dependencies
    └── Dockerfile             # Container configuration
```

## Documentation

For more information about Azure API Management multi-region deployment, see the [official documentation](https://learn.microsoft.com/en-us/azure/api-management/api-management-howto-deploy-multi-region).

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution.
