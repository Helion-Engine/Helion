#!/bin/bash

# We want to copy Rules.ruleset into all of the directories with a .csproj, at
# least until we figure out why it's not being respected on Linux w/ Travis...
echo "Running TravisCI helper script"
for D in *; do
    if [ -d "${D}" ]; then
	count=`ls -l ${D}/*.csproj 2>/dev/null | wc -l`
	if [ $count != 0 ]
	then
        	echo "Folder to copy to: ${D}"
		cp Rules.ruleset ${D}/
	fi
    fi
done

