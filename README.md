# DebugProbe.AspNetCore

Debug ASP.NET Core requests and responses instantly. Compare API calls across environments.

## Features
1. Capture HTTP requests & responses
2. View headers, query, and body
3. Compare responses between environments
4. Simple UI endpoint (/debug)
5. Minimal setup

## Install

    dotnet add package DebugProbe.AspNetCore

**Usage**

Register services

      builder.Services.AddDebugProbe();

Add middleware
   
      app.UseDebugProbe();
      
Open UI
   
      /debug

Intended for development/debugging only. Do not use in production without restrictions

License

MIT
