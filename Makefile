restore:
	dotnet restore src

publish:
	dotnet publish --configuration Release --output app src

watch:
	dotnet watch run --project src
