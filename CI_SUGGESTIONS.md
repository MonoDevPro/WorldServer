# CI/CD Configuration Suggestions for WorldServer

Este documento fornece sugestões para configuração de CI/CD que melhoram a **manutenibilidade**, **segurança** e **qualidade** do código do WorldServer.

## 1. Pipeline Básico Sugerido

### GitHub Actions Workflow (.github/workflows/ci.yml)

```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        dotnet-version: ['8.0.x']
    
    steps:
    - uses: actions/checkout@v4
      with:
        submodules: recursive # Para QuadTrees submodule
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ matrix.dotnet-version }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Run Unit Tests
      run: dotnet test --no-build --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload Coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        file: coverage.xml
        fail_ci_if_error: false

  code-quality:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
      with:
        submodules: recursive
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Install dotnet format
      run: dotnet tool install -g dotnet-format
    
    - name: Check code formatting
      run: dotnet format --verify-no-changes --verbosity diagnostic
    
    - name: Run Security Analysis (optional)
      run: |
        dotnet tool install --global security-scan
        security-scan --project ./WorldServer.sln

  performance-test:
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    steps:
    - uses: actions/checkout@v4
      with:
        submodules: recursive
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Build Release
      run: dotnet build --configuration Release
    
    - name: Run Stress Tests (Light)
      run: |
        # Execute stress test framework created in the project
        # This would need a specific test runner implementation
        echo "Running light stress tests..."
        dotnet test --configuration Release --filter "Category=StressTest&Priority=Light"
```

## 2. Configurações de Qualidade de Código

### .editorconfig
```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
indent_style = space
indent_size = 4

[*.{cs,csx,vb,vbx}]
indent_size = 4

[*.{json,js,html,css}]
indent_size = 2

[*.cs]
# Code style rules
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Performance rules
dotnet_diagnostic.CA1822.severity = suggestion # Member can be marked as static
dotnet_diagnostic.CA1825.severity = warning   # Avoid zero-length array allocations
dotnet_diagnostic.CA1829.severity = warning   # Use Length/Count property instead of Count() when available
```

### Directory.Build.props (para configurações globais)
```xml
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors>CS1998;CS8602;CS0436</WarningsNotAsErrors>
    
    <!-- Enable nullable reference types -->
    <Nullable>enable</Nullable>
    
    <!-- Code Analysis -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    
    <!-- Performance -->
    <ServerGarbageCollection>true</ServerGarbageCollection>
    <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
  </PropertyGroup>
</Project>
```

## 3. Monitoramento e Métricas

### Application Insights / OpenTelemetry
```yaml
# adicionar ao CI para validar telemetria
- name: Validate Telemetry Configuration
  run: |
    dotnet add package Microsoft.ApplicationInsights.AspNetCore
    # Validate that performance monitoring is properly configured
    grep -r "PerformanceMonitor" ./Simulation.Application/Services/
```

### Dockerfile para Testes de Integração
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 9050

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN git submodule update --init --recursive
RUN dotnet restore
RUN dotnet build -c Release

FROM build AS test
RUN dotnet test --logger trx --results-directory /testresults

FROM build AS publish
RUN dotnet publish "Simulation.Server/Simulation.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Simulation.Server.dll"]
```

## 4. Validações de Segurança

### Dependencias e Vulnerabilidades
```yaml
- name: Security Audit
  run: |
    dotnet list package --vulnerable --include-transitive
    dotnet list package --deprecated
```

### Configuração de Secrets
```yaml
# Para configurações de produção
- name: Deploy with Secrets
  env:
    CONNECTIONSTRING: ${{ secrets.DATABASE_CONNECTION }}
    APIKEY: ${{ secrets.EXTERNAL_API_KEY }}
  run: |
    # Deploy com configurações seguras
```

## 5. Sugestões de Branching Strategy

### GitFlow Simplificado
- `main`: Produção estável
- `develop`: Desenvolvimento ativo
- `feature/*`: Novas funcionalidades
- `hotfix/*`: Correções urgentes

### Proteções de Branch
```yaml
# Configurar no GitHub
# Settings > Branches > Add rule para main:
# - Require pull request reviews before merging
# - Require status checks to pass before merging
# - Require branches to be up to date before merging
# - Include administrators
```

## 6. Performance e Monitoramento Contínuo

### Benchmarking Automático
```yaml
- name: Run Benchmarks
  run: |
    dotnet run --project Simulation.Benchmarks --configuration Release
    # Compare with baseline performance
```

### Métricas de Build
- Tempo de build
- Cobertura de testes
- Complexidade ciclomática
- Dívida técnica (SonarQube)

## 7. Deployment Automático

### Staging Environment
```yaml
deploy-staging:
  needs: [build-and-test, code-quality]
  runs-on: ubuntu-latest
  if: github.ref == 'refs/heads/develop'
  steps:
  - name: Deploy to Staging
    run: |
      # Deploy automático para ambiente de staging
      # Validação automática com smoke tests
```

## 8. Alertas e Notificações

### Configurações de Notificação
- Build quebrado: Notificar no Slack/Teams
- Vulnerabilidades detectadas: Email para equipa de segurança
- Performance degradada: Alerta automático

## Resumo de Benefícios

✅ **Manutenibilidade**: Formatação automática, análise de código  
✅ **Segurança**: Audit de vulnerabilidades, proteção de secrets  
✅ **Performance**: Benchmarking automático, monitoramento contínuo  
✅ **Qualidade**: Testes automáticos, cobertura de código  
✅ **Estabilidade**: Deployment automático com validações  

Esta configuração garante que cada mudança no código passa por validações rigorosas antes de ser integrada, mantendo a qualidade e estabilidade do WorldServer.