# PowerShell profile loaded inside the container.

# Better directory listings.
Set-Alias ll Get-ChildItem

# Common git shortcuts.
function gs {
	git status
}

function gl {
	git log --oneline --graph --decorate -20
}

# Improve terminal experience.
$PSStyle.FileInfo.Directory = "`e[36m"