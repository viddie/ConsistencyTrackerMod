$NAMEARG=$args[0]
$MODNAME
while (!$MODNAME) {
    if ($NAMEARG) {
        $MODNAME = $NAMEARG
    } else {
        $MODNAME = Read-Host "Enter Mod Name"
    }
    Clear-Variable NAMEARG

    if (!($MODNAME -match "^([a-zA-Z0-9]| |_|-)*$")) {
        Write-Output "!!! Name contains invalid characters."
        Write-Output "Valid characters include ``a-zA-Z0-9 _-``"
        Clear-Variable MODNAME
        continue
    }

    Write-Output "Checking for duplicates..."
    if (!($UPDATELIST)){
        $UPDATELIST = (Invoke-WebRequest (Invoke-WebRequest https://everestapi.github.io/modupdater.txt).Content).Content
    }

    ForEach ($MOD in $($UPDATELIST -split "`r`n")){
        if ($MOD -match "((\r\n|\r|\n)|^)$MODNAME`:"){
            Write-Output "Name already in use!"
            Clear-Variable MODNAME
            break
        }
    }
}

Get-ChildItem -exclude .* -force | ForEach-Object {
    Get-ChildItem -path $_.Name | ForEach-Object {
        (Get-Content $_).replace('$MODNAME$', $MODNAME) | Out-File -Encoding "utf8" $_
        Move-Item $_.FullName ($_.DirectoryName + '/' + ($_.Name.replace("`$MODNAME`$",($MODNAME))))
    }
    if ($_.Name -match "^(init.(ps1|sh|bat)|README.md|LICENSE)"){
        Remove-Item -Force $_
    }
}
Write-Output "Initialization complete."