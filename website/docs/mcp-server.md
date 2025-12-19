---
sidebar_position: 5
---

# MCP Server
Topaz comes with a dedicated MCP server, which you can use to simplify interaction with the emulator. 

## Configuration
Reference [VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers) documentation to see the detailed instruction on how to add MCP server to your environment.

### Adding Topaz MCP server in VS Code
In `.vscode` directory create a file `mcp.json` with the following content:
```json
{
  "servers": {
    "TopazMCPDocker": {
      "type": "stdio",
      "command": "docker",
      "args": [
        "run",
        "-i",
        "thecloudtheory/topaz-mcp:<version>"
      ]
    }
  }
}
```
Where `<version>` is the selected image tag. After a moment the server should start and be available for your use.