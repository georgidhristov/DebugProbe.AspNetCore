# DebugProbe.AspNetCore

Lightweight tool for inspecting ASP.NET Core HTTP requests and responses.  
Capture, analyze, and compare API calls directly in your application.

## Features
1. Capture HTTP requests & responses
2. Inspect headers, query, and body
3. Pretty JSON formatting
4. Compare responses across environments
5. Built-in UI endpoint (/debug)
6. Minimal setup (no configuration required)

## Install

    dotnet add package DebugProbe.AspNetCore

**Usage**

Register services

    builder.Services.AddDebugProbe();

Add middleware
   
    app.UseDebugProbe();
      
Open UI
   
    /debug

Compare responses

    Use the UI to compare a local request with another environment by providing:
    - Base URL
    - Trace ID

Intended for development and debugging only.  
Do not use in production without proper security (authentication, filtering, access control).

License

MIT
