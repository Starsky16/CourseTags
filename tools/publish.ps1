if (Test-Path -Path "./bin") {
    Remove-Item ./bin -recurse
}
dotnet publish CourseTags.csproj -c Release -p:CreateCipx=true
