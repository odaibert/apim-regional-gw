# Multi-Region API Management Architecture

```mermaid
graph TB
    Users((Global Users))
    TM[Traffic Manager<br/>Geographic Routing]
    subgraph Primary[Primary Region - East US]
        APIM1[API Management<br/>Premium SKU]
        BE1[Mock Backend<br/>Service]
        APIM1 --> BE1
    end
    subgraph Secondary[Secondary Region - West Europe]
        APIM2[API Management<br/>Premium SKU]
        BE2[Mock Backend<br/>Service]
        APIM2 --> BE2
    end
    Users --> TM
    TM --> APIM1
    TM --> APIM2
    
    classDef azure fill:#0072C6,stroke:#fff,stroke-width:2px,color:#fff;
    classDef user fill:#232F3E,stroke:#fff,stroke-width:2px,color:#fff;
    class TM,APIM1,APIM2,BE1,BE2 azure;
    class Users user;

    %% Notes
    note1[Geographic routing based on<br/>user location with automatic<br/>failover support]
    note2[Health monitoring via<br/>status endpoint checks]
    note3[Synchronized configuration<br/>with primary region]
    
    style note1 fill:#fff2cc,stroke:#d6b656
    style note2 fill:#fff2cc,stroke:#d6b656
    style note3 fill:#fff2cc,stroke:#d6b656
    
    TM -.-> note1
    APIM1 -.-> note2
    APIM2 -.-> note3
```

## Key Components

1. **Traffic Manager**
   - Geographic routing method
   - Health monitoring of regional endpoints
   - Automatic failover support

2. **Primary Region (East US)**
   - Premium SKU API Management instance
   - Mock backend service
   - Status endpoint monitoring

3. **Secondary Region (West Europe)**
   - Premium SKU API Management instance
   - Mock backend service
   - Synchronized with primary region

## Traffic Flow
1. Users connect to the nearest API Management instance based on their geographic location
2. Traffic Manager routes requests using geographic routing rules
3. Each regional API Management instance processes requests and forwards them to backend services
4. Health monitoring ensures high availability across regions

## Security Note
- All endpoints use secure HTTPS communication
- Regional status endpoints are monitored by Traffic Manager
- Authentication and authorization handled by API Management
- Configuration synchronized securely between regions
