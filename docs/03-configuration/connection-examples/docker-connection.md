# 🐳 Docker 연결

> **컨테이너 환경에서 MCP Server Framework를 실행하고 연결하는 완전한 가이드**

Docker를 사용한 MCP Server 배포는 **일관된 실행 환경**과 **손쉬운 확장성**을 제공합니다. 이 문서에서는 Docker 컨테이너로 MCP Server를 실행하고 다양한 방식으로 연결하는 방법을 다룹니다.

## 🎯 Docker 사용의 장점

### **배포 관점**
- ✅ **환경 일관성**: 개발/테스트/프로덕션 동일 환경
- ✅ **빠른 배포**: 이미지 빌드 후 어디서든 실행
- ✅ **격리성**: 호스트 시스템으로부터 격리된 실행
- ✅ **확장성**: 여러 컨테이너 인스턴스 쉽게 실행

### **운영 관점**
- ✅ **자동 복구**: 컨테이너 장애 시 자동 재시작
- ✅ **리소스 제한**: CPU, 메모리 사용량 제어
- ✅ **로그 통합**: 중앙화된 로그 관리
- ✅ **버전 관리**: 이미지 태그를 통한 버전 관리

## 🐳 Docker 이미지 빌드

### **1. 기본 Docker 이미지**

프로젝트에 포함된 `docker/Dockerfile`을 사용합니다:

```dockerfile
# Use the official .NET runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the official .NET SDK as a build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["../src/Micube.MCP.Server/Micube.MCP.Server.csproj", "Micube.MCP.Server/"]
COPY ["../src/Micube.MCP.Core/Micube.MCP.Core.csproj", "Micube.MCP.Core/"]
COPY ["../src/Micube.MCP.SDK/Micube.MCP.SDK.csproj", "Micube.MCP.SDK/"]

RUN dotnet restore "Micube.MCP.Server/Micube.MCP.Server.csproj"

# Copy everything else and build
COPY ../src/ .
RUN dotnet build "Micube.MCP.Server/Micube.MCP.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Micube.MCP.Server/Micube.MCP.Server.csproj" -c Release -o /app/publish

# Final stage/image
FROM base AS final
WORKDIR /app

# Create necessary directories
RUN mkdir -p /app/tools /app/docs /app/prompts /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://*:5000

# Create non-root user for security
RUN groupadd -r mcpuser && useradd -r -g mcpuser mcpuser
RUN chown -R mcpuser:mcpuser /app
USER mcpuser

ENTRYPOINT ["dotnet", "Micube.MCP.Server.dll"]
```

### **2. 이미지 빌드**

```bash
# docker 디렉토리에서 빌드
cd docker
docker build -f Dockerfile -t mcp-server:latest .

# 태그 추가
docker tag mcp-server:latest mcp-server:v1.0.0

# 빌드 확인
docker images | grep mcp-server
```

### **3. 멀티 아키텍처 빌드**

```bash
# ARM64와 AMD64를 모두 지원하는 이미지 빌드
docker buildx create --name multiarch-builder
docker buildx use multiarch-builder

docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -t mcp-server:multi-arch \
  --push \
  -f Dockerfile .
```

## 🚀 컨테이너 실행

### **1. 기본 실행**

```bash
# 기본 실행 (HTTP 모드)
docker run -d \
  --name mcp-server \
  -p 5000:5000 \
  mcp-server:latest

# 실행 확인
curl http://localhost:5000/health
```

### **2. 볼륨 마운트 실행**

```bash
# 설정과 로그 디렉토리 마운트
docker run -d \
  --name mcp-server \
  -p 5000:5000 \
  -v $(pwd)/docker/config:/app/config:ro \
  -v $(pwd)/docker/logs:/app/logs \
  -v $(pwd)/docker/tools:/app/tools:ro \
  -v $(pwd)/docker/docs:/app/docs:ro \
  -v $(pwd)/docker/prompts:/app/prompts:ro \
  mcp-server:latest
```

### **3. 환경 변수 설정**

```bash
# 환경 변수로 설정 오버라이드
docker run -d \
  --name mcp-server \
  -p 5000:5000 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Logging__MinLevel=Info \
  -e Features__EnableStdio=false \
  -e Features__EnableHttp=true \
  mcp-server:latest
```

## 🐙 Docker Compose 활용

### **1. 기본 docker-compose.yml**

프로젝트에 포함된 `docker/docker-compose.yml`:

```yaml
version: '3.8'

services:
  mcp-server:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    container_name: mcp-server
    image: micube.mcp.server:1.0.0
    ports:
      - "5000:5000"
    volumes:
      # 설정 파일 마운트
      - ./config:/app/config:ro
      # 로그 디렉토리 마운트
      - ./logs:/app/logs
      # 커스텀 도구들 마운트
      - ./tools:/app/tools:ro
      # 문서 리소스 마운트
      - ./docs:/app/docs:ro
      # 프롬프트 마운트
      - ./prompts:/app/prompts:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://*:5000
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    restart: unless-stopped

networks:
  default:
    name: mcp-network
```

### **2. Docker Compose 실행**

```bash
# 서비스 시작
cd docker
docker compose up -d

# 로그 확인
docker compose logs -f

# 서비스 중지
docker compose down

# 이미지까지 제거
docker compose down --rmi all
```

### **3. 개발용 Compose 설정**

```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  mcp-server:
    build:
      context: ..
      dockerfile: docker/Dockerfile
      target: build  # 개발 단계에서 멈춤
    container_name: mcp-server-dev
    image: micube.mcp.server:1.0.0
    ports:
      - "5000:5000"
    volumes:
      - ../src:/src  # 소스 코드 마운트
      - ./config:/app/config:ro
      - ./logs:/app/logs
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - DOTNET_USE_POLLING_FILE_WATCHER=true
    command: ["dotnet", "watch", "run", "--project", "/src/Micube.MCP.Server"]
    restart: unless-stopped
```

## 🔌 컨테이너 연결 방법

### **1. Docker Compose 연결 (가장 권장)**

Docker Compose는 가장 간단하고 관리하기 쉬운 방법입니다.

#### **기본 docker-compose.yml 설정**
```yaml
# docker-compose.yml
version: '3.8'

services:
  mcp-server:
    build:
      context: ..
      dockerfile: docker/Dockerfile
    container_name: mcp-server
    stdin_open: true        # -i 옵션
    tty: false             # STDIO 모드용
    volumes:
      - ./config:/app/config:ro
      - ./logs:/app/logs
      - ./tools:/app/tools:ro
      - ./docs:/app/docs:ro
      - ./prompts:/app/prompts:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Features__EnableStdio=true
      - Features__EnableHttp=false
    restart: "no"          # Claude Desktop이 관리
```

#### **Claude Desktop 설정 (claude_desktop_config.json)**
```json
{
  "mcpServers": {
    "mcp-server-compose": {
      "command": "docker",
      "args": [
        "compose",
        "-f", "C:\\path\\to\\mcp-server\\docker\\docker-compose.yml",
        "run", "--rm", "mcp-server"
      ],
      "env": {}
    }
  }
}
```

#### **크로스 플랫폼 설정**
```json
{
  "mcpServers": {
    "mcp-compose-windows": {
      "command": "docker",
      "args": [
        "compose", "-f", "C:\\tools\\mcp-server\\docker-compose.yml",
        "run", "--rm", "mcp-server"
      ]
    },
    "mcp-compose-unix": {
      "command": "docker",
      "args": [
        "compose", "-f", "/opt/mcp-server/docker-compose.yml", 
        "run", "--rm", "mcp-server"
      ]
    }
  }
}
```

### **2. STDIO 전용 Compose 설정**

#### **stdio-compose.yml**
```yaml
# docker/stdio-compose.yml - STDIO 전용 최적화
version: '3.8'

services:
  mcp-server:
    image: mcp-server:latest
    stdin_open: true
    tty: false
    volumes:
      - ./config:/app/config:ro
      - ./logs:/app/logs
      - ./tools:/app/tools:ro  
      - ./docs:/app/docs:ro
      - ./prompts:/app/prompts:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Features__EnableStdio=true
      - Features__EnableHttp=false
      - Logging__MinLevel=Info
    # 네트워크 불필요 (STDIO만 사용)
    network_mode: none
    # 리소스 제한
    mem_limit: 512m
    cpus: 1.0
```

#### **Claude Desktop에서 사용**
```json
{
  "mcpServers": {
    "mcp-stdio": {
      "command": "docker",
      "args": [
        "compose", "-f", "docker/stdio-compose.yml",
        "run", "--rm", "mcp-server"  
      ]
    }
  }
}
```

### **3. HTTP 서비스 Compose 설정**

#### **http-compose.yml**
```yaml
# docker/http-compose.yml - HTTP 서비스용
version: '3.8'

services:
  mcp-server:
    image: mcp-server:latest
    ports:
      - "5000:5000"
    volumes:
      - ./config:/app/config:ro
      - ./logs:/app/logs
      - ./tools:/app/tools:ro
      - ./docs:/app/docs:ro
      - ./prompts:/app/prompts:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Features__EnableStdio=false
      - Features__EnableHttp=true
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    restart: unless-stopped
```

#### **서비스 시작 및 클라이언트 연결**
```bash
# HTTP 서버 시작
docker compose -f docker/http-compose.yml up -d

# 다른 터미널에서 클라이언트 연결
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"TestClient","version":"1.0"},"capabilities":{}}}'
```

### **4. 개발 환경 Compose 설정**

#### **dev-compose.yml**
```yaml
# docker/dev-compose.yml - 개발 환경용
version: '3.8'

services:
  mcp-dev:
    build:
      context: ..
      dockerfile: docker/Dockerfile
      target: build  # 빌드 스테이지에서 중단
    stdin_open: true
    tty: false
    volumes:
      - ../src:/src
      - ./config:/app/config:ro
      - ./logs:/app/logs
    working_dir: /src
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - DOTNET_USE_POLLING_FILE_WATCHER=true
    command: ["dotnet", "run", "--project", "Micube.MCP.Server"]
```

#### **Claude Desktop 개발 설정**
```json
{
  "mcpServers": {
    "mcp-development": {
      "command": "docker",
      "args": [
        "compose", "-f", "docker/dev-compose.yml",
        "run", "--rm", "mcp-dev"
      ],
      "env": {
        "DOTNET_USE_POLLING_FILE_WATCHER": "true"
      }
    }
  }
}
```

### **5. 간단한 Docker run 연결 (대안)**

Docker Compose가 없는 환경에서만 사용:

```json
{
  "mcpServers": {
    "mcp-simple-docker": {
      "command": "docker",
      "args": [
        "run", "--rm", "-i", "--init",
        "--name", "mcp-server-claude",
        "-v", "C:\\tools\\mcp-server\\config:/app/config:ro",
        "-v", "C:\\tools\\mcp-server\\logs:/app/logs",
        "-e", "Features__EnableStdio=true",
        "-e", "Features__EnableHttp=false",
        "mcp-server:latest"
      ]
    }
  }
}
```

#### **호스트에서 연결**
```bash
# 컨테이너 실행
docker run -d -p 5000:5000 --name mcp-server mcp-server:latest

# 호스트에서 테스트
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "initialize",
    "params": {
      "protocolVersion": "2025-06-18",
      "clientInfo": {"name": "Docker Test", "version": "1.0"},
      "capabilities": {}
    }
  }'
```

#### **다른 컨테이너에서 연결**
```yaml
# docker-compose.yml
version: '3.8'

services:
  mcp-server:
    build: .
    container_name: mcp-server
    networks:
      - mcp-network

  mcp-client:
    image: node:18
    container_name: mcp-client
    networks:
      - mcp-network
    volumes:
      - ./client:/app
    working_dir: /app
    command: ["node", "client.js"]
    depends_on:
      - mcp-server

networks:
  mcp-network:
```

```javascript
// client/client.js
const McpClient = require('./mcp-client');

const client = new McpClient('http://mcp-server:5000'); // 컨테이너 이름 사용
client.initialize().then(() => {
    console.log('Connected to MCP server');
});
```

### **2. 네트워크 모드별 연결**

#### **Bridge 네트워크 (기본)**
```bash
# 컨테이너 간 통신
docker network create mcp-bridge
docker run -d --network mcp-bridge --name mcp-server mcp-server:latest
docker run -it --network mcp-bridge alpine/curl \
  curl http://mcp-server:5000/health
```

#### **Host 네트워크**
```bash
# 호스트 네트워크 직접 사용
docker run -d --network host --name mcp-server mcp-server:latest

# localhost로 직접 접근 가능
curl http://localhost:5000/health
```

## 🛡️ 보안 설정

### **1. 비루트 사용자 실행**

Dockerfile에서 이미 설정됨:
```dockerfile
# Create non-root user for security
RUN groupadd -r mcpuser && useradd -r -g mcpuser mcpuser
RUN chown -R mcpuser:mcpuser /app
USER mcpuser
```

### **2. 읽기 전용 파일 시스템**

```bash
# 읽기 전용 루트 파일 시스템
docker run -d \
  --read-only \
  --tmpfs /tmp \
  --tmpfs /var/tmp \
  -v $(pwd)/logs:/app/logs \
  -p 5000:5000 \
  mcp-server:latest
```

### **3. 리소스 제한**

```bash
# CPU 및 메모리 제한
docker run -d \
  --name mcp-server \
  --memory=512m \
  --cpus=1.0 \
  --memory-swap=512m \
  -p 5000:5000 \
  mcp-server