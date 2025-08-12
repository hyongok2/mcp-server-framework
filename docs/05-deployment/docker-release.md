# 🐳 Docker 이미지 수동 배포 가이드

이 문서는 Docker Hub를 사용하지 않고, 로컬에서 빌드한 MCP Server Framework Docker 이미지를 파일로 저장한 뒤 다른 시스템으로 복사하여 배포하는 절차를 정리한 문서입니다.

---

## ✅ 0. Docker Compose로 이미지 빌드

`docker-compose.yml`이 위치한 경로에서 아래 명령을 실행하여 이미지를 빌드합니다.

```bash
docker compose build
```

> 📁 예시 디렉토리 구조:
> 
> ```
> /mcp-server-framework
> ├── src/
> │   ├── Micube.MCP.Server/
> │   ├── Micube.MCP.Core/
> │   └── Tools/SampleTools/
> ├── docker/
> │   ├── docker-compose.yml
> │   └── Dockerfile
> ```

> ✅ `docker-compose.yml`에는 `build:`와 `dockerfile:` 경로가 다음과 같이 정의되어 있어야 합니다:

```yaml
services:
  mcp-server:
    build:
      context: ../
      dockerfile: docker/Dockerfile
    image: mcp-server:latest
    # image: micube.mcp.server:1.0.0
    # 또는 버전 태그
    # image: mcp-server:1.0.0
```

---

## ✅ 1. Docker 이미지 저장

로컬에서 빌드된 Docker 이미지를 `.tar` 파일로 저장합니다.

```bash
# 최신 버전 저장
docker save -o mcp-server-latest.tar mcp-server:latest

# 특정 버전 저장
docker save -o mcp-server-1.0.0.tar mcp-server:1.0.0
```

* `-o` 옵션으로 파일명을 지정
* `mcp-server:latest`는 이미지 이름과 태그

### 📦 도구 포함 배포 패키지 생성

```bash
# 이미지와 설정 파일을 함께 패키징
tar -czf mcp-server-deployment-1.0.0.tar.gz \
    mcp-server-1.0.0.tar \
    docker/config/ \
    docker/tools/ \
    docker/docs/ \
    docker/prompts/ \
    docker/docker-compose.yml
```

* **주의** 반드시 ToolGroup DLL 파일은 연관된 모든 DLL 파일과 함께 tools폴더에 저장해야 합니다.
* Tool에서 사용하는 IP 중에 LocalHost가 있는 경우, Docker 기반에서는 반드시 **host.docker.internal**로 변경 사용해야 한다.

---

## ✅ 2. 저장된 파일 복사

USB, 공유 폴더, SCP 등으로 다른 시스템에 복사합니다.

```bash
# SCP 사용 예시
scp mcp-server-deployment-1.0.0.tar.gz user@target-server:/opt/mcp-server/

# 배포 패키지 압축 해제
tar -xzf mcp-server-deployment-1.0.0.tar.gz
```

---

## ✅ 3. 다른 시스템에서 이미지 로드

복사된 시스템에서 Docker 이미지 파일을 로드합니다.

```bash
# Docker 이미지 로드
docker load -i mcp-server-1.0.0.tar

# 로드된 이미지 확인
docker images | grep mcp-server
```

---

## ✅ 4. 이미지 실행 (직접 또는 Compose 사용)

### 1) Docker 명령어로 직접 실행 (STDIO 모드)  (Docker로 STDIO를 사용하는 것은 권장하지 않습니다.)

```bash
# 기본 실행
docker run --rm -i \
    --name mcp-server-manual \
    -v $(pwd)/config:/app/config:ro \
    -v $(pwd)/tools:/app/tools:ro \
    -v $(pwd)/docs:/app/docs:ro \
    -v $(pwd)/prompts:/app/prompts:ro \
    -v $(pwd)/logs:/app/logs \
    mcp-server:1.0.0
```

### 2) Docker 명령어로 HTTP 모드 실행

```bash
# HTTP 서버 모드
docker run -d \
    --name mcp-server-http \
    -p 5555:5555 \
    -v $(pwd)/config:/app/config:ro \
    -v $(pwd)/tools:/app/tools:ro \
    -v $(pwd)/docs:/app/docs:ro \
    -v $(pwd)/prompts:/app/prompts:ro \
    -v $(pwd)/logs:/app/logs \
    -e Features__EnableStdio=false \
    -e Features__EnableHttp=true \
    mcp-server:1.0.0
```

### 3) docker-compose 사용

```yaml
# docker-compose.yml
services:
  mcp-server:
    # 배포시에는 build 항목은 삭제한다.
    build:
      context: ..
      dockerfile: docker/Dockerfile
    container_name: mcp-server
    image: micube.mcp.server:1.0.0
    extra_hosts:
      - "host.docker.internal:host-gateway" # 이렇게 하면 호스트 IP 설정이 필요한 곳에서는  host.docker.internal <- 이렇게 쓰면 된다.  
    # Docker 사용 시에는 Http 기준으로만 사용한다.
    ports:
      - "5555:5555"
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
      - ASPNETCORE_URLS=http://*:5555
    healthcheck:
      test: ["CMD", "curl", "-f", "http://host.docker.internal:5555/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    restart: "no"  

networks:
  default:
    name: mcp-network
```

**실행 명령:**
```bash
docker compose -f docker-compose.yml up -d
```

---

## ✅ 5. 배포 후 검증

### 헬스체크 (HTTP 모드)
```bash
curl http://localhost:5555/health
```

### STDIO 모드 테스트
```bash
# 간단한 초기화 테스트
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"TestClient","version":"1.0"},"capabilities":{}}}' | \
docker exec -i mcp-server-prod cat
```

### 도구 목록 확인 (HTTP 모드)
```bash
curl -X POST http://localhost:5555/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

---

## ✅ 기타 참고사항

### 이미지 크기 최적화
```bash
# 이미지 크기 확인
docker images mcp-server

# 불필요한 이미지 정리
docker image prune

# 압축률 향상을 위해 gzip 사용
gzip mcp-server-1.0.0.tar
```

---
