# Validation de la capture multi-écrans et haute résolution DPI

Ce document décrit l'implémentation et la matrice d'acceptation du ticket GitHub #4. Il distingue les tests géométriques automatisés des validations physiques sur écrans réels : des tests géométriques réussis ne prouvent pas que toutes les configurations DPI de Windows ont été testées sur matériel.

## Modifications apportées

- Le manifeste déclare désormais la prise en charge DPI Per-Monitor V2 (dpiAwareness=PerMonitorV2), avec le repli historique true/pm. Le projet conserve WPF .NET 8 et ApplicationHighDpiMode=PerMonitorV2.
- La sélection de zone crée une fenêtre de sélection par écran physique au lieu d'étirer une seule fenêtre WPF sur le bureau virtuel. Chaque HWND est positionné avec les coordonnées physiques natives de l'écran et WPF effectue son rendu avec le DPI propre à cet écran.
- Le curseur est lu avec GetCursorPos : les coordonnées de début et de fin du glissement restent donc des pixels physiques, même lorsque la sélection traverse des écrans ayant des facteurs d'échelle différents.
- Chaque fenêtre convertit le même rectangle physique global vers ses propres DIPs WPF. Le rectangle transmis au service de capture reste en coordonnées physiques.
- La capture complète calcule les limites du bureau virtuel à partir de tous les écrans, y compris ceux dont l'origine est négative ou situés au-dessus/en dessous de l'écran principal. La capture de zone arrondit vers l'extérieur afin de conserver les pixels de bord et limite la zone au bureau virtuel avant CopyFromScreen.
- Les métadonnées des captures indiquent désormais la véritable origine du bureau virtuel ou la zone réellement capturée.
- Le HUD d'enregistrement, la notification de capture et les captures épinglées sont placés sur l'écran situé sous le curseur. Le placement natif utilise le DPI effectif de la fenêtre cible et la zone de travail physique de l'écran.
- Le calcul du placement des fenêtres est isolé dans une fonction testable et couvre les origines négatives, les alignements, les décalages et les zones de travail réduites.

## Preuves avant / après

| Élément | Avant | Après |
|---|---|---|
| Sélecteur de zone | Une fenêtre WPF utilisait les dimensions physiques du bureau virtuel comme des DIPs WPF | Une fenêtre native par écran, chacune rendue à son propre DPI |
| Coordonnées du glissement | Coordonnées locales WPF additionnées à Left/Top, mélangeant DIPs et pixels physiques | Coordonnées globales du curseur via GetCursorPos, rectangle en pixels physiques |
| Écrans à DPI différents | Une fenêtre unique ne pouvait pas représenter simultanément plusieurs échelles | Chaque écran dessine la sélection globale avec sa propre transformation PointFromScreen |
| Bureau virtuel | Les métadonnées d'origine étaient toujours (0, 0) | Les limites réelles conservent les décalages positifs et négatifs |
| Dimensions d'une zone | Math.Round pouvait perdre des pixels de bord et la zone n'était pas limitée | Arrondi extérieur, découpage au bureau virtuel et dimensions réelles dans les métadonnées |
| Fenêtres flottantes | Utilisation de SystemParameters.WorkArea de l'écran principal | Zone de travail et DPI de l'écran sous le curseur |

## Vérification automatisée

Depuis le dossier de la solution :

~~~powershell
dotnet test .\ScreenTools.sln -c Release --no-restore -m:1 -nodeReuse:false
dotnet build .\ScreenTools.sln -c Release --no-restore -m:1 -nodeReuse:false
~~~

Les tests couvrent les géométries multi-écrans, les origines négatives, les bords fractionnaires, le découpage et le placement. Chaque écran connecté est sondé physiquement au centre et aux quatre coins ; les jonctions contiguës sont capturées lorsque la disposition le permet. Les sorties PNG/JPG/BMP sont comparées aux dimensions physiques du bureau ; les JPEG des profils d'enregistrement sont vérifiés en largeur et en ratio. La suite contient actuellement 97 tests.

Ces tests vérifient la géométrie pure et le comportement existant du service de capture. Ils ne simulent pas le véritable flux Windows WM_DPICHANGED de WPF et ne remplacent pas les essais physiques avec plusieurs DPI.

## Configuration d'écrans relevée pendant ce changement

Rapport généré sur la machine de développement avec tools/Get-DisplayCaptureDiagnostics.ps1 :

| Élément | Valeur observée |
|---|---|
| Système | Windows 11 Famille, build 26300, x64 |
| Écrans | 2 |
| Écran 1 | 1920 × 1080, origine (0, 0), principal, 96 DPI / 100 % |
| Écran 2 | 1920 × 1080, origine (1920, 0), secondaire, 96 DPI / 100 % |
| Bureau virtuel | 3840 × 1080, origine (0, 0) |
| Disposition | Côté à côté, même résolution et échelle 100 % |

Il s'agit d'un relevé de configuration, pas d'une affirmation selon laquelle toutes les lignes ont été testées manuellement dans l'application. La machine ne fournit actuellement pas d'écran à 125 % ou 150 %, de disposition verticale ni d'écrans à DPI différents.

Pour collecter la configuration d'une machine de test :

~~~powershell
.\tools\Get-DisplayCaptureDiagnostics.ps1 -OutputPath .\BenchmarkResults\display-diagnostics.json
~~~

Le rapport JSON contient la version/build Windows, l'architecture du processus, le nombre d'écrans, leurs limites physiques et zones de travail, leur DPI/échelle effective et les limites du bureau virtuel.

## Matrice d'acceptation manuelle

| ID | Disposition | DPI | Scénario | Critère de réussite |
|---|---|---|---|---|
| D01 | Un écran 1920×1080 | 100 % | Capture complète et capture de zone | Dimensions identiques aux pixels physiques de l'écran/de la sélection |
| D02 | Deux écrans côte à côte | 100 % / 100 % | Sélection sur chaque écran et traversée de la jonction | Sélection continue, sans décalage ni changement d'échelle |
| D03 | Deux écrans de résolutions différentes | 100 % / 100 % | Bureau complet et zone traversant les écrans | Limites virtuelles et dimensions de sortie exactes |
| D04 | Écran secondaire à gauche | 100 % | Capture sur origine X négative | Origine négative conservée et pixels corrects |
| D05 | Écran secondaire au-dessus | 100 % | Capture sur origine Y négative | Pas de découpage ni de décalage |
| D06 | Écran secondaire en dessous | 100 % | Capture sur écran inférieur | Curseur, loupe et sélection alignés |
| D07 | Un écran | 125 % | Sélection, capture complète et HUD | Aucun décalage de coordonnées ×1,25 |
| D08 | Un écran | 150 % | Sélection, capture complète et HUD | Aucun décalage ×1,5 ; dimensions en pixels physiques |
| D09 | Deux écrans, même résolution | 100 % / 150 % | Glissement de 100 % vers 150 % | Rectangle continu et dimensions exactes |
| D10 | Deux écrans, résolutions différentes | 125 % / 150 % | Capture complète, zone, HUD et notification | Aucun découpage, mauvais placement ou dimension incorrecte |
| D11 | Disposition multi-écrans | Mixte | Enregistrement et dimensions exportées | Enregistrement du bureau complet conforme au bureau virtuel après redimensionnement du profil |
| D12 | Toute disposition multi-écrans | Mixte | Déconnexion/reconnexion ou réorganisation d'un écran | Les nouvelles limites sont utilisées, sans coordonnées obsolètes |

### Périmètre de la zone d'enregistrement

L'application ne propose actuellement pas de sélection de zone vidéo. L'enregistrement et la relecture instantanée capturent tout le bureau virtuel ; la sélection de zone ne concerne que les captures d'écran. D11 vérifie donc les dimensions de l'enregistrement complet. Une zone vidéo sélectionnable est une fonctionnalité distincte et ne doit pas être déclarée implémentée ou testée par ce ticket.

## État de l'acceptation

- [x] DPI Per-Monitor V2 déclaré dans le manifeste et la configuration du projet.
- [x] Fenêtre de sélection par écran avec limites physiques natives.
- [x] Coordonnées physiques globales lors du glissement entre écrans.
- [x] Limites du bureau virtuel compatibles avec les origines négatives.
- [x] Arrondi extérieur et découpage des zones.
- [x] Métadonnées réelles des captures complètes et des zones.
- [x] HUD et fenêtres flottantes placés sur l'écran du curseur avec son DPI.
- [x] Tests géométriques et de placement automatisés réussis.
- [x] Compilation Release de la solution réussie.
- [ ] Essais physiques sur un écran à 125 % et 150 %.
- [ ] Essais physiques multi-écrans à DPI/résolutions mixtes.
- [ ] Essais physiques avec écrans au-dessus/en dessous et origines négatives.
- [ ] Captures manuelles du sélecteur, du HUD et des résultats sur ces configurations.
- [ ] Validation physique des écrans sous Windows 10.

Ne cochez les essais physiques qu'après inspection du rapport de diagnostic et des fichiers réellement capturés sur le matériel correspondant.
