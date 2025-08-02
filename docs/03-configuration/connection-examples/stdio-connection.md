# 📡 STDIO 연결

> **표준 입출력을 통한 직접 프로세스 통신 방식의 MCP 연결을 마스터합니다**

STDIO(Standard Input/Output) 연결은 MCP의 **가장 일반적이고 안전한 연결 방식**입니다. 클라이언트가 MCP 서버를 직접 프로세스로 실행하고 stdin/stdout을 통해 통신합니다.

## 🎯 STDIO 연결의 특징

### **장점**
- ✅ **직접 통신**: 네트워크 오버헤드 없음
- ✅ **높은 보안**: 로컬 프로세스 통신으로 외부 노출 없음
- ✅ **낮은 지연시간**: 파이프 기반 고속 통신
- ✅ **자동 생명주기**: 클라이언트 종료 시 서버도 자동 종료

### **단점**
- ❌ **단일 클라이언트**: 한 번에 하나의 클라이언트만 연결
- ❌ **로컬 제한**: 원격 서버 연결 불가
- ❌ **디버깅 어려움**: 표준 출력이 통신에 사용됨

### **적합한 사용 사례**
- Claude Desktop 연결
- VS Code 확장
- 로컬 개발 도구
- 개인용 AI 어시스턴트

## 🛠️ 서버 설정

### **1. appsettings.json 설정**
```json
{
  "Features": {
    "EnableStdio": true,      // STDIO 활성화 (필수)
    "EnableHttp": false       // HTTP 비활성화 (보안 강화)
  },
  "Logging": {
    "MinLevel": "Info"        // Debug 로그 비활성화 (STDIO 출력 간섭 방지)
  },
  "ToolGroups": {
    "Directory": "tools",
    "Whitelist": ["SampleTools.dll"]
  }
}
```

### **2. 실행 확인**
```bash
# 서버가 STDIO 모드에서 정상 작동하는지 테스트
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","clientInfo":{"name":"TestClient","version":"1.0"},"capabilities":{}}}' | dotnet run --project src/Micube.MCP.Server
```

**예상 출력:**
```json
{"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2025-06-18","serverInfo":{"name":"Micube MCP Server Framework","version":"0.1.0"},"capabilities":{"tools":{"listChanged":false},"resources":{"subscribe":false,"listChanged":false},"prompts":{"listChanged":false}}}}
```

## 🎭 Claude Desktop 연결

### **1. 설정 파일 위치**
- **Windows**: `%APPDATA%\Claude\claude_desktop_config.json`
- **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Linux**: `~/.config/Claude/claude_desktop_config.json`

### **2. 개발 환경 설정**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\dev\\mcp-server-framework\\src\\Micube.MCP.Server"
      ],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### **3. 빌드된 실행 파일 설정**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "C:\\Program Files\\MCPServer\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### **4. 크로스 플랫폼 설정**

#### **Windows**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "C:\\tools\\mcp-server\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "PATH": "C:\\tools\\mcp-server;%PATH%"
      }
    }
  }
}
```

#### **macOS/Linux**
```json
{
  "mcpServers": {
    "mcp-server-framework": {
      "command": "/usr/local/bin/mcp-server/Micube.MCP.Server",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "PATH": "/usr/local/bin/mcp-server:$PATH"
      }
    }
  }
}
```

## 💻 VS Code 확장 연결

### **1. 개발 환경 설정**
프로젝트 루트의 `.vscode/settings.json`:
```json
{
  "mcp.servers": [
    {
      "name": "MCP Server Framework",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/Micube.MCP.Server"
      ],
      "cwd": "${workspaceFolder}",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### **2. 전역 설정**
사용자 설정의 `settings.json`:
```json
{
  "mcp.servers": [
    {
      "name": "Global MCP Server",
      "command": "C:\\tools\\mcp-server\\Micube.MCP.Server.exe",
      "args": [],
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  ]
}
```

## 🐍 Python 클라이언트 구현

### **1. 기본 STDIO 클라이언트**
```python
import subprocess
import json
import threading
from queue import Queue

class McpStdioClient:
    def __init__(self, command, args=None):
        self.command = command
        self.args = args or []
        self.process = None
        self.response_queue = Queue()
        self.request_id = 0
        
    def start(self):
        """서버 프로세스 시작"""
        self.process = subprocess.Popen(
            [self.command] + self.args,
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=0
        )
        
        # 응답 읽기 스레드 시작
        self.reader_thread = threading.Thread(target=self._read_responses)
        self.reader_thread.daemon = True
        self.reader_thread.start()
    
    def _read_responses(self):
        """응답 읽기 스레드"""
        while self.process and self.process.poll() is None:
            try:
                line = self.process.stdout.readline()
                if line:
                    response = json.loads(line.strip())
                    self.response_queue.put(response)
            except Exception as e:
                print(f"Error reading response: {e}")
    
    def send_request(self, method, params=None):
        """요청 전송"""
        self.request_id += 1
        request = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": method,
            "params": params or {}
        }
        
        request_json = json.dumps(request) + "\n"
        self.process.stdin.write(request_json)
        self.process.stdin.flush()
        
        return self.request_id
    
    def get_response(self, timeout=5):
        """응답 받기"""
        try:
            return self.response_queue.get(timeout=timeout)
        except:
            return None
    
    def initialize(self):
        """서버 초기화"""
        request_id = self.send_request("initialize", {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "Python STDIO Client",
                "version": "1.0.0"
            },
            "capabilities": {}
        })
        
        response = self.get_response()
        return response
    
    def call_tool(self, tool_name, arguments):
        """도구 호출"""
        request_id = self.send_request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })
        
        response = self.get_response()
        return response
    
    def stop(self):
        """서버 프로세스 종료"""
        if self.process:
            self.process.terminate()
            self.process.wait()

# 사용 예시
if __name__ == "__main__":
    # 클라이언트 생성 및 시작
    client = McpStdioClient(
        "dotnet", 
        ["run", "--project", "src/Micube.MCP.Server"]
    )
    
    try:
        client.start()
        
        # 초기화
        init_response = client.initialize()
        print("Initialize response:", init_response)
        
        # 도구 호출
        tool_response = client.call_tool("Echo_Echo", {
            "text": "Hello from Python STDIO client!"
        })
        print("Tool response:", tool_response)
        
    finally:
        client.stop()
```

### **2. 고급 기능이 포함된 클라이언트**
```python
import asyncio
import json
from typing import Dict, Any, Optional

class AsyncMcpStdioClient:
    def __init__(self, command: str, args: list = None):
        self.command = command
        self.args = args or []
        self.process = None
        self.request_id = 0
        self.pending_requests: Dict[int, asyncio.Future] = {}
        
    async def start(self):
        """비동기로 서버 시작"""
        self.process = await asyncio.create_subprocess_exec(
            self.command, *self.args,
            stdin=asyncio.subprocess.PIPE,
            stdout=asyncio.subprocess.PIPE,
            stderr=asyncio.subprocess.PIPE
        )
        
        # 응답 읽기 태스크 시작
        asyncio.create_task(self._read_responses())
    
    async def _read_responses(self):
        """응답 읽기 루프"""
        while True:
            try:
                line = await self.process.stdout.readline()
                if not line:
                    break
                    
                response = json.loads(line.decode().strip())
                request_id = response.get('id')
                
                if request_id in self.pending_requests:
                    future = self.pending_requests.pop(request_id)
                    future.set_result(response)
                    
            except Exception as e:
                print(f"Error reading response: {e}")
    
    async def send_request(self, method: str, params: Optional[Dict[str, Any]] = None):
        """비동기 요청 전송"""
        self.request_id += 1
        request = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": method,
            "params": params or {}
        }
        
        # Future 등록
        future = asyncio.Future()
        self.pending_requests[self.request_id] = future
        
        # 요청 전송
        request_json = json.dumps(request) + "\n"
        self.process.stdin.write(request_json.encode())
        await self.process.stdin.drain()
        
        # 응답 대기
        return await future
    
    async def initialize(self):
        """서버 초기화"""
        return await self.send_request("initialize", {
            "protocolVersion": "2025-06-18",
            "clientInfo": {
                "name": "Async Python STDIO Client",
                "version": "1.0.0"
            },
            "capabilities": {}
        })
    
    async def call_tool(self, tool_name: str, arguments: Dict[str, Any]):
        """도구 호출"""
        return await self.send_request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })
    
    async def stop(self):
        """서버 종료"""
        if self.process:
            self.process.terminate()
            await self.process.wait()

# 사용 예시
async def main():
    client = AsyncMcpStdioClient(
        "dotnet", 
        ["run", "--project", "src/Micube.MCP.Server"]
    )
    
    try:
        await client.start()
        
        # 초기화
        init_response = await client.initialize()
        print("Initialize response:", init_response)
        
        # 여러 도구 동시 호출
        tasks = [
            client.call_tool("Echo_Echo", {"text": f"Message {i}"})
            for i in range(3)
        ]
        
        responses = await asyncio.gather(*tasks)
        for i, response in enumerate(responses):
            print(f"Response {i}:", response)
        
    finally:
        await client.stop()

if __name__ == "__main__":
    asyncio.run(main())
```

## 🔧 Node.js/JavaScript 클라이언트

### **1. 기본 STDIO 클라이언트**
```javascript
const { spawn } = require('child_process');
const { EventEmitter } = require('events');

class McpStdioClient extends EventEmitter {
    constructor(command, args = []) {
        super();
        this.command = command;
        this.args = args;
        this.process = null;
        this.requestId = 0;
        this.pendingRequests = new Map();
    }

    start() {
        return new Promise((resolve, reject) => {
            this.process = spawn(this.command, this.args, {
                stdio: ['pipe', 'pipe', 'pipe']
            });

            this.process.stdout.on('data', (data) => {
                const lines = data.toString().trim().split('\n');
                lines.forEach(line => {
                    if (line) {
                        try {
                            const response = JSON.parse(line);
                            this.handleResponse(response);
                        } catch (e) {
                            console.error('Failed to parse response:', line);
                        }
                    }
                });
            });

            this.process.stderr.on('data', (data) => {
                console.error('Server stderr:', data.toString());
            });

            this.process.on('error', reject);
            this.process.on('spawn', resolve);
        });
    }

    handleResponse(response) {
        const id = response.id;
        if (this.pendingRequests.has(id)) {
            const { resolve } = this.pendingRequests.get(id);
            this.pendingRequests.delete(id);
            resolve(response);
        }
    }

    sendRequest(method, params = {}) {
        return new Promise((resolve, reject) => {
            this.requestId++;
            const request = {
                jsonrpc: '2.0',
                id: this.requestId,
                method,
                params
            };

            this.pendingRequests.set(this.requestId, { resolve, reject });

            const requestJson = JSON.stringify(request) + '\n';
            this.process.stdin.write(requestJson);

            // 타임아웃 설정
            setTimeout(() => {
                if (this.pendingRequests.has(this.requestId)) {
                    this.pendingRequests.delete(this.requestId);
                    reject(new Error('Request timeout'));
                }
            }, 5000);
        });
    }

    async initialize() {
        return await this.sendRequest('initialize', {
            protocolVersion: '2025-06-18',
            clientInfo: {
                name: 'Node.js STDIO Client',
                version: '1.0.0'
            },
            capabilities: {}
        });
    }

    async callTool(toolName, arguments) {
        return await this.sendRequest('tools/call', {
            name: toolName,
            arguments
        });
    }

    stop() {
        if (this.process) {
            this.process.kill();
        }
    }
}

// 사용 예시
async function main() {
    const client = new McpStdioClient('dotnet', [
        'run', '--project', 'src/Micube.MCP.Server'
    ]);

    try {
        await client.start();
        console.log('Server started');

        const initResponse = await client.initialize();
        console.log('Initialize response:', initResponse);

        const toolResponse = await client.callTool('Echo_Echo', {
            text: 'Hello from Node.js!'
        });
        console.log('Tool response:', toolResponse);

    } catch (error) {
        console.error('Error:', error);
    } finally {
        client.stop();
    }
}

main();
```

## 🔍 디버깅 및 문제 해결

### **1. 로그 분석**
STDIO 모드에서는 표준 출력이 통신에 사용되므로 로그는 파일로만 확인할 수 있습니다:

```bash
# 로그 파일 위치 (appappsettings.json 확인)
```

### **2. 일반적인 문제들**

#### **문제 1: 프로세스 시작 실패**
```bash
# 증상: "No such file or directory"
# 해결: 경로 확인
which dotnet
ls -la src/Micube.MCP.Server/Micube.MCP.Server.csproj
```

#### **문제 2: JSON 파싱 오류**
```json
// ❌ 잘못된 요청 (줄바꿈 누락)
{"jsonrpc":"2.0","id":1,"method":"initialize"}

// ✅ 올바른 요청
{"jsonrpc":"2.0","id":1,"method":"initialize"}\n
```

#### **문제 3: 응답 타임아웃**
```bash
# 서버 프로세스 상태 확인
ps aux | grep Micube.MCP.Server

# 좀비 프로세스 정리
pkill -f Micube.MCP.Server
```

---

**다음**: [HTTP 연결](http-connection.md) - 웹 API 기반 연결 방식 →

**이전**: [← 클라이언트 연결](../client-connection.md)