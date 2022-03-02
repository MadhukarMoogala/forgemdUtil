---
typora-root-url: ./
---

# Forge MD Utility

### Contents

* [Description](#description)
* [Setup](#setup)
* [Using the Application](#using-the-application)
* [Tips and Tricks](#tips-and-tricks)
* [License](#license)

## Description

This code sample demonstrates the simple automated usage of translating source\seed file to SVF2 considering the Region of storage, derivates and server endpoint.

### Thumbnail

![Thumbnail](https://github.com/MadhukarMoogala/forgemdUtil/blob/master/thumbnail.JPG)

## Setup

### Prerequisites
* [Visual Studio](https://code.visualstudio.com/): Either Community 2019+ (Windows) or Code (Windows, MacOS).
* [dotNET 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
* Basic Knowledge of C#
* Autodesk Forge Account: Learn how to create a Forge Account, activate subscription and create an app at this tutorial

### Running locally
Clone this project or download it. It's recommended to install [GitHub desktop](https://desktop.github.com/). To clone it via command line, use the following (**Terminal** on MacOSX/Linux, **Git Shell** on Windows):
- create a setenv.bat
```bash
git clone https://github.com/MadhukarMoogala/forgemdUtil.git
cd forgemdUtil
devenv forgemdTest.sln
```
- Initialize and install dependencies

```
dotnet build
set FORGE_CLIENT_ID=[YOUR_CLIENT_ID]
set FORGE_CLIENT_SECRET=[YOUR_CLIENT_SECRET]
SET FORGE_API_PATH=https://developer.api.autodesk.com
dotnet run run "files\Hall.ifc"
```
### Debugging Locally
* **Visual Studio** (Windows):
 - Open the solution project forgemdTest.sln
 - Update `Property\launchSettings.json` with `FORGE_CLIENT_ID`, `FORGE_CLIENT_SECRET` and `FORGE_API_PATH`
 ```bash
 {
  "profiles": {
    "forgemdTest": {
      "commandName": "Project",
      "commandLineArgs": "",
      "environmentVariables": {
        "FORGE_API_PATH": "",
        "FORGE_CLIENT_ID": "",
        "FORGE_CLIENT_SECRET": ""
      }
    }
  }
}
 ```

### License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](https://github.com/MadhukarMoogala/forgemdUtil/blob/master/LICENSE) file for full details.

### Written by

Madhukar Moogala, [Forge Partner Development](http://forge.autodesk.com/) @galakar

