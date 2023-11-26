Push-Location "$PSScriptRoot/.."
try
{
	rm ./TestResults/* -Recurse
	dotnet test --collect "XPlat Code Coverage" --results-directory "./TestResults"

	$mergedCoverage = "./TestResults/coverage.cobertura.xml"

	dotnet-coverage merge coverage.cobertura.xml -r -o $mergedCoverage -f cobertura

	reportgenerator -reports:$mergedCoverage -targetDir:"./TestResults/coverageReport"

	./TestResults/coverageReport/index.html
}
finally
{
	Pop-Location
}
