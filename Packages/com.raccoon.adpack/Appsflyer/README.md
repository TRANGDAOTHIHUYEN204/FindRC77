# Add appsflyer
- Add package manager: https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git#upm
- Add Script Compilation: ENABLE_APPSFLYER
# Configure
- Create object in scene and add component GameAppsFlyer
- Add App ID in inspector (Android = Package name, iOS = appId)
- Add App Dev Key in inspector
# Use
- Call GameAppsFlyer.SendAdRevenue()