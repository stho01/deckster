# Testing
Run the following unit test to test connecting to an existing chat game and sending a chat message.
Swap the "gameId" to an existing gameId and "userId" to a userId not already connected to the game.

`.\gradlew decksterlib:test --info -DuserId=frode7 -Dpassword=1234 -DgameId=5dc0f6303ace4bfa9bcca976dcc81b37`

Prerequisites: A conforming `decksterapi.yml` in the repository root folder.

# Generating DTO classes

Generating DTO classes will automatically be done as part of the build process. 

The YML file with DTO definition must be named `decksterapi.yml` and reside in the git repo root.
You can download it from http://localhost:13992/meta/messages.yaml when running the dotnet server.
The output foder will be `decksterlib/src-gen`

If you want to trigger code generation manually, run gradle task `openApiPostProcess`. 
It depends on `generateDtos` which depends on `openApiPreProcess`.

The pre- and postprocess steps are custom hacks to support package names in the YML file, specified
by a single dot-separator in the schema type names.

Syntax from `/android` folder:  
Windows:  
`./gradlew openaApiPostProcess`

Linux/Mac:
`./gradle openaApiPostProcess`


