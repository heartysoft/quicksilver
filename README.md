# quicksilver
Quick build + deploy platform for .NET. Currently supports msdeploy for websites and topshelf for windows services. Takes care of build, test, packaging, tokenisation and single command deployment.

## Current Features

* Build from sln file(s), run nunit tests, package and publish package to a location.
* For web projects, MSDeploy packaging is used.
* Expects Windows Service projects to use TopShelf.
* Published artifact is a zip folder containing files necessary for single command deployment.
* Single declarative build config description file and bootstrapper to add to project. Additional environment files for tokenization during deployment.
* Support for additional tokenization files at deployment time.
* Single command to build, test, package and publish. Deployment package is created if git tag with a v* pattern is present in the current commit.

## Future Features

* NuGet packaging.
* Versioning of AssemblyInfo, etc.
* Additional test frameworks, etc.

## Usage

Please check the wiki.

## Examples

Will add here later. (Check wiki for now).
