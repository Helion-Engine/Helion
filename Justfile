build:
	dotnet publish -c Release -r linux-x64 --self-contained=true -p:AOT=true
install-system-wide: build
	ln -s Publish/linux-x64_AOT/Helion /usr/local/bin
	cp Assets/Misc/helion.svg /usr/share/icons/hicolor/scalable/apps/Helion.svg
	cp Assets/Misc/Helion.desktop /usr/share/applications/Helion.desktop
	gtk-update-icon-cache /usr/share/icons/hicolor
install-single-user: build
	ln -s Publish/linux-x64_AOT/Helion ~/.local/bin
	mkdir -p ~/.local/share/icons/hicolor/scalable/apps/
	cp Assets/Misc/helion.svg ~/.local/share/icons/hicolor/scalable/apps/Helion.svg
	cp Assets/Misc/Helion.desktop ~/.local/share/applications/Helion.desktop
	gtk-update-icon-cache /usr/share/icons/hicolor
