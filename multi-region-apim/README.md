# Multi-Region API Management Deployment

This project deploys an Azure API Management service in multiple regions using Premium SKU for improved availability and reduced latency.

## Project Structure

```
multi-region-apim/
├── bicep/
│   └── main.bicep         # Bicep template for multi-region APIM deployment
└── notebooks/
    ├── scripts/
    │   ├── deploy_multi_region_apim.py    # Deployment script template
    │   └── cleanup_resources.py           # Resource cleanup script template
    ├── deploy-multi-region-apim.ipynb     # Deployment notebook
    └── cleanup-resources.ipynb            # Resource cleanup notebook
```

## Prerequisites

- Azure subscription with Contributor permissions
- Azure CLI installed and authenticated
- Python 3.12 or later
- Required Python packages: azure-mgmt-apimanagement, pandas, matplotlib

## Configuration

The deployment supports configuring:
- Azure subscription ID
- Primary and secondary regions
- API Management SKU (Premium required for multi-region)
- Resource naming convention
