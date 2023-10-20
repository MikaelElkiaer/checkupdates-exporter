build: restore publish build-docker

restore:
	dotnet restore src

publish:
	dotnet publish --no-restore --configuration Release --output app src

build-docker:
	docker build . --file docker/local.Dockerfile --tag ghcr.io/mikaelelkiaer/checkupdates-exporter:local

watch:
	dotnet watch run --project src --launch-profile web
