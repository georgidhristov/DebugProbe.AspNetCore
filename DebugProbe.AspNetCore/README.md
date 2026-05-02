# DebugProbe.AspNetCore ![NuGet](https://img.shields.io/nuget/v/DebugProbe.AspNetCore) ![Downloads](https://img.shields.io/nuget/dt/DebugProbe.AspNetCore) ![License](https://img.shields.io/badge/license-MIT-blue) 

<p align="left">
  <img src="https://raw.githubusercontent.com/georgidhristov/DebugProbe.AspNetCore/master/Assets/logo.png" width="120" />
</p>

**Inspect and compare HTTP traffic directly inside your ASP.NET Core app.**

No proxies. No external tools. Just plug in and debug.



## Why DebugProbe?

- Debug real requests from inside your app
- No proxy setup or traffic interception
- See exactly what your API sends and receives
- Compare environments in seconds



## Features

- Capture HTTP requests & responses
- Inspect headers, query, and body
- JSON pretty formatting
- Side-by-side response comparison
- Built-in UI (`/debug`)
- Zero configuration



## Screenshots

### Requests
![Requests](https://raw.githubusercontent.com/georgidhristov/DebugProbe.AspNetCore/master/Assets/requests.png)

### Details
![Details](https://raw.githubusercontent.com/georgidhristov/DebugProbe.AspNetCore/master/Assets/details.png)

### Compare
![Compare](https://raw.githubusercontent.com/georgidhristov/DebugProbe.AspNetCore/master/Assets/compare.png)

---

## Install

```bash
dotnet add package DebugProbe.AspNetCore
```

## Quick Start
```bash
builder.Services.AddDebugProbe();

//Add middleware 
app.UseDebugProbe();
```

## Open Debug UI
Run your application, then open:

http://localhost:{port}/debug


## Compare Responses

Use the UI to compare responses across environments:

- Enter **Base URL**
- Enter **Trace ID**
- Instantly see differences

## ⚠️ Production Usage

This tool is intended for development.

If used in production:

- Add authentication
- Restrict access
- Filter sensitive data


## License

MIT
