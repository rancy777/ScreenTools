# Contribuer à ScreenTools

**Documentation :** [English](CONTRIBUTING.md) · [Français](CONTRIBUTING.fr.md) · [简体中文](CONTRIBUTING.zh-CN.md)

Merci de vouloir contribuer au projet. Avant de commencer, lisez [README.md](README.md) et [REMAINING_WORK.md](REMAINING_WORK.md), consultez les tickets ouverts et indiquez sur quel sujet vous souhaitez travailler. Pour toute évolution architecturale, présentez l’approche envisagée ainsi que ses compromis avant de préparer une modification importante.

## Environnement de développement

Exécutez les commandes de la solution depuis son dossier :

```powershell
Push-Location .\ScreenTools
try {
    dotnet restore .\ScreenTools.sln
    dotnet build .\ScreenTools.sln -c Release
    dotnet test .\ScreenTools.sln -c Release
} finally {
    Pop-Location
}
```

Après toute modification de la capture, de l’encodage ou du tampon de relecture, lancez également les tests de performance :

```powershell
Push-Location .\ScreenTools
try {
    dotnet run --project ..\ScreenTools.Benchmarks\ScreenTools.Benchmarks.csproj -c Release -- all --iterations 60
} finally {
    Pop-Location
}
```

Privilégiez des modifications ciblées et ne versionnez aucun fichier généré ou propre à une machine. Ne commitez jamais les dossiers `bin` et `obj`, les fichiers temporaires de projet WPF, les archives locales, les enregistrements, les résultats de mesure des performances, les paramètres personnels ou un exécutable `ffmpeg.exe`.

## Pull requests

Veuillez fournir :

- Le problème traité et la solution retenue.
- Les éléments volontairement laissés hors du périmètre.
- Les étapes de reproduction et de test, ainsi que leurs résultats.
- La version de Windows et les informations matérielles utiles pour les problèmes de capture.
- Des captures d’écran ou une courte vidéo pour les changements visibles de l’interface.
- La méthode de mesure et les résultats avant/après pour les changements de performance.

Pour toute modification de l’enregistrement, testez le démarrage et l’arrêt, la pause et la reprise, plusieurs sessions successives, la lecture du fichier produit et le nettoyage. Pour la relecture instantanée, indiquez la durée demandée et la durée réellement exportée. Pour la persistance, testez les fichiers valides, les JSON mal formés, les écritures interrompues et la récupération sans toucher au profil utilisateur réel.

## Périmètre et priorités

À ce stade, améliorer la fiabilité est plus important que d’ajouter des fonctionnalités. Les priorités sont la validation sur du matériel réel, la capture du son système, la stabilité de l’enregistrement, la validation de la relecture instantanée, le comportement multi-écrans/DPI et les tests automatisés.