# Generating DTO classes

Generating DTO classes will automatically be done as part of the build process. 

The YML file with DTO definition must be named `openapi.yml` and reside in the git repo root.
You can download it from http://localhost:13992/meta/messages.yaml when running the dotnet server.
The output foder will be `decksterlib/src-gen`

If you want to trigger code generation manually, run gradle task `openaApiPostProcess`. 
It depends on `generateDtos` which depends on `openApiPreProcess`.

The pre- and postprocess steps are custom hacks to support package names in the YML file, specified
by a single dot-separator in the schema type names.

Syntax from `/android` folder:  
Windows:  
`./gradlew openaApiPostProcess`

Linux/Mac:
`./gradle openaApiPostProcess`

