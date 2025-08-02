# Docker 사용 가이드

## 🚀 빠른 시작

### 1. 이미지 빌드
```bash
# docker 디렉토리에서
docker build -f Dockerfile -t mcp-server:latest .
```

### 2. 컨테이너 실행
```bash
# 기본 실행
docker run -d -p 5000:5000 --name mcp-server mcp-server:latest

# 볼륨 마운트와 함께 실행
docker run -d -p 5000:5000 --name mcp-server \
  -v $(pwd)/logs:/app/logs \
  -v $(pwd)/src/Micube.MCP.Server/config:/app/config:ro \
  mcp-server:latest
```

### 3. Docker Compose 사용 (권장)
```bash
# docker 디렉토리에서
cd docker
docker compose up -d

# 컨테이너 중지
docker compose down

# 이미지까지 삭제
docker compose down --rmi all

# docker 빌드
docker compose build

```

### 헬스체크
```bash
curl http://localhost:5000/health
```

## 🔧 설정

### 환경 변수
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: 바인딩 URL (기본: http://*:5000)

### 볼륨 마운트
- `/app/config`: 설정 파일
- `/app/logs`: 로그 파일
- `/app/tools`: 도구 DLL
- `/app/docs`: 리소스 파일
- `/app/prompts`: 프롬프트 템플릿
