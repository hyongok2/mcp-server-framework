# 🌐 HTTP 연결

> **HTTP API를 통한 웹 기반 MCP 연결 방식을 완전히 마스터합니다**

HTTP 연결은 MCP Server Framework의 **확장성 있는 연결 방식**으로, 여러 클라이언트가 동시에 접근할 수 있고 원격 서버 연결이 가능합니다. 웹 애플리케이션과 마이크로서비스 아키텍처에 최적화되어 있습니다.

## 🎯 HTTP 연결의 특징

### **장점**
- ✅ **다중 클라이언트**: 여러 클라이언트 동시 연결 가능
- ✅ **원격 접근**: 네트워크를 통한 원격 서버 연결
- ✅ **표준 프로토콜**: HTTP/HTTPS 기반 보편적 접근
- ✅ **확장성**: 로드 밸런서, 프록시 등과 연동 가능
- ✅ **디버깅 용이**: 표준 HTTP 도구로 쉽게 테스트

### **단점**
- ❌ **네트워크 지연**: TCP/HTTP 오버헤드
- ❌ **보안 고려**: 네트워크 노출로 인한 보안 이슈
- ❌ **리소스 사용**: 더 많은 메모리와 CPU 사용

### **적합한 사용 사례**
- 웹 애플리케이션 백엔드
- 마이크로서비스 간 통신
- 클라우드 기반 AI 서비스
- 다중 사용자 환경

## 🛠️ 서버 설정

### **1. 기본 HTTP 설정**
```json
{
  "Features": {
    "EnableStdio": false,     // STDIO 비활성화 (선택사항)
    "EnableHttp": true        // HTTP 활성화 (필수)
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  },
  "Logging": {
    "MinLevel": "Info"        // 웹 환경에서는 모든 로그 레벨 사용 가능
  }
}
```

### **2. HTTPS 보안 설정**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001",
        "Certificate": {
          "Path": "certificates/mcp-server.pfx",
          "Password": "${CERT_PASSWORD}"
        }
      }
    }
  }
}
```

### **3. 프로덕션 설정**
```json
{
  "Features": {
    "EnableStdio": false,
    "EnableHttp": true
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://0.0.0.0:443"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxRequestBodySize": 10485760,     // 10MB
      "RequestHeadersTimeout": "00:00:30",
      "KeepAliveTimeout": "00:02:00"
    }
  }
```

### **4. 서버 시작 및 확인**
```bash
# 서버 시작
dotnet run

# 헬스체크로 확인
curl http://localhost:5000/health

# 예상 응답
{
  "status": "healthy",
  "timestamp": "2025-01-15T10:30:00Z",
  "version": "0.1.0"
}
```

## 🔧 클라이언트 구현

### **Python 클라이언트**

#### **기본 HTTP 클라이언트**
```python
import requests
import json
from typing import Dict, Any, Optional, List

class McpHttpClient:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url
        self.request_id = 0
        self.is_initialized = False
        self.session = requests.Session()
        
    def initialize(self) -> Dict[str, Any]:
        """서버 초기화"""
        response = self.send_request("initialize", {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "Python HTTP Client",
                "version": "1.0.0"
            },
            "capabilities": {}
        })
        
        self.is_initialized = response.get("result") is not None
        return response
    
    def send_request(self, method: str, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """HTTP 요청 전송"""
        self.request_id += 1
        
        request_data = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": method,
            "params": params or {}
        }
        
        try:
            response = self.session.post(
                f"{self.base_url}/mcp",
                json=request_data,
                headers={"Content-Type": "application/json"},
                timeout=30
            )
            response.raise_for_status()
            return response.json()
            
        except requests.exceptions.RequestException as e:
            raise Exception(f"Request failed: {e}")
    
    def list_tools(self) -> List[Dict[str, Any]]:
        """도구 목록 조회"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("tools/list")
        return response.get("result", {}).get("tools", [])
    
    def call_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Any:
        """도구 호출"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })
        return response.get("result")
    
    def list_resources(self) -> List[Dict[str, Any]]:
        """리소스 목록 조회"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("resources/list")
        return response.get("result", {}).get("resources", [])
    
    def read_resource(self, uri: str) -> str:
        """리소스 읽기"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("resources/read", {"uri": uri})
        contents = response.get("result", {}).get("contents", [])
        return contents[0].get("text", "") if contents else ""
    
    def list_prompts(self) -> List[Dict[str, Any]]:
        """프롬프트 목록 조회"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("prompts/list")
        return response.get("result", {}).get("prompts", [])
    
    def get_prompt(self, name: str, arguments: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """프롬프트 실행"""
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = self.send_request("prompts/get", {
            "name": name,
            "arguments": arguments or {}
        })
        return response.get("result", {})

# 사용 예시
def main():
    client = McpHttpClient("http://localhost:5000")
    
    try:
        # 초기화
        init_response = client.initialize()
        print("Initialized:", init_response)
        
        # 도구 목록 및 호출
        tools = client.list_tools()
        print(f"Found {len(tools)} tools")
        
        if tools:
            echo_result = client.call_tool("Echo_Echo", {
                "text": "Hello from Python!"
            })
            print("Echo result:", echo_result)
        
        # 리소스 목록 및 읽기
        resources = client.list_resources()
        print(f"Found {len(resources)} resources")
        
        if resources:
            content = client.read_resource(resources[0]["uri"])
            print("Resource content preview:", content[:100] + "...")
        
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main()
```

#### **비동기 Python 클라이언트**
```python
import aiohttp
import asyncio
import json
from typing import Dict, Any, Optional, List

class AsyncMcpHttpClient:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url
        self.request_id = 0
        self.is_initialized = False
        self.session: Optional[aiohttp.ClientSession] = None
    
    async def __aenter__(self):
        self.session = aiohttp.ClientSession()
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self.session:
            await self.session.close()
    
    async def initialize(self) -> Dict[str, Any]:
        response = await self.send_request("initialize", {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "Async Python HTTP Client",
                "version": "1.0.0"
            },
            "capabilities": {}
        })
        
        self.is_initialized = response.get("result") is not None
        return response
    
    async def send_request(self, method: str, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        if not self.session:
            raise Exception("Session not initialized")
        
        self.request_id += 1
        
        request_data = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": method,
            "params": params or {}
        }
        
        try:
            async with self.session.post(
                f"{self.base_url}/mcp",
                json=request_data,
                headers={"Content-Type": "application/json"},
                timeout=aiohttp.ClientTimeout(total=30)
            ) as response:
                response.raise_for_status()
                return await response.json()
                
        except aiohttp.ClientError as e:
            raise Exception(f"Request failed: {e}")
    
    async def call_tool(self, tool_name: str, arguments: Dict[str, Any]) -> Any:
        if not self.is_initialized:
            raise Exception("Client not initialized")
            
        response = await self.send_request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })
        return response.get("result")

# 사용 예시
async def async_example():
    async with AsyncMcpHttpClient("http://localhost:5000") as client:
        # 초기화
        await client.initialize()
        
        # 병렬 도구 호출
        tasks = [
            client.call_tool("Echo_Echo", {"text": f"Message {i}"})
            for i in range(5)
        ]
        
        results = await asyncio.gather(*tasks)
        for i, result in enumerate(results):
            print(f"Result {i}: {result}")

# 실행
asyncio.run(async_example())
```


---

**다음**: [Docker 연결](docker-connection.md) - 컨테이너 환경에서의 연결 설정 →

**이전**: [← STDIO 연결](stdio-connection.md)