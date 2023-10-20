build: restore publish build-docker

build-docker:
	docker build . --file docker/local.Dockerfile --tag ghcr.io/mikaelelkiaer/checkupdates-exporter:local

publish:
	dotnet publish --no-restore --configuration Release --output app src

restore:
	dotnet restore src

test:
	dotnet test tests

watch:
	dotnet watch run --project src --launch-profile web
