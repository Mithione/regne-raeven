// Regneræven on Azure – as cheap as possible:
//  - Azure Container Apps (Consumption, scales to zero, 0.25 vCPU / 0.5 GiB) – within the monthly free grant
//  - Azure SQL Database free offer (serverless, auto-pauses, stops instead of billing when the free amount is used)
//  - no Log Analytics workspace (logs cost money); use "az containerapp logs show" when needed
// Deploy: see docs/azure.md

@description('Azure region. Choose one in the EU – the app stores children\'s data.')
param location string = resourceGroup().location

@description('Short name used in all resource names.')
@maxLength(12)
param name string = 'regneraeven'

@description('Container image, e.g. ghcr.io/mithione/regne-raeven:latest')
param image string

@description('Container registry (ghcr.io for GitHub Container Registry).')
param registryServer string = 'ghcr.io'

@description('User name for the registry (your GitHub user name).')
param registryUsername string

@description('Token that can read the image (GitHub personal access token with read:packages).')
@secure()
param registryPassword string

@description('SQL administrator login.')
param sqlAdminLogin string = 'regneraevenadmin'

@description('SQL administrator password (at least 12 characters, upper and lower case, numbers).')
@secure()
param sqlAdminPassword string

@description('Code new families must enter to create an account. Empty = anyone can sign up.')
@secure()
param inviteCode string = ''

var suffix = uniqueString(resourceGroup().id)
var sqlServerName = '${name}-sql-${suffix}'
var databaseName = name

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Lets Azure services (the container app) reach the database. No other IP addresses are allowed.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Azure SQL free offer: 100,000 vCore seconds and 32 GB per month, free for the lifetime of the subscription.
resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause' // never bills – pauses until next month instead
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${name}-env'
  location: location
  properties: {
    // No appLogsConfiguration → no Log Analytics workspace and no log costs.
  }
}

var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${databaseName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=60'

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: registryServer
          username: registryUsername
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        { name: 'registry-password', value: registryPassword }
        { name: 'db-connection', value: connectionString }
        { name: 'invite-code', value: inviteCode }
      ]
    }
    template: {
      containers: [
        {
          name: 'regneraeven'
          image: image
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'ConnectionStrings__Regneraeven', secretRef: 'db-connection' }
            { name: 'Registration__InviteCode', secretRef: 'invite-code' }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/healthz', port: 8080 }
              initialDelaySeconds: 15
              periodSeconds: 30
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0 // scale to zero when nobody uses it → no cost
        maxReplicas: 1
        rules: [
          {
            name: 'http'
            http: { metadata: { concurrentRequests: '30' } }
          }
        ]
      }
    }
  }
  dependsOn: [ database, allowAzure ]
}

output url string = 'https://${app.properties.configuration.ingress.fqdn}'
output sqlServer string = sqlServer.properties.fullyQualifiedDomainName
