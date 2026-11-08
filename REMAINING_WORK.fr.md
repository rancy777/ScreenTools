# Travail restant

**Documentation :** [English](REMAINING_WORK.md) · [Français](REMAINING_WORK.fr.md) · [简体中文](REMAINING_WORK.zh-CN.md)

## État actuel

ScreenTools dispose d’une base fonctionnelle pour les captures d’écran, l’historique du presse-papiers, l’enregistrement vidéo, la relecture instantanée, la capture du son système via WASAPI sans Stereo Mix et la création de packages MSIX. Les travaux d’ingénierie P1 et P2, ainsi que le nettoyage P3, les tests de régression et l’outil de mesure des performances, sont implémentés. Le projet n’est toutefois pas encore prêt pour la production : les longues sessions et la compatibilité matérielle entre versions de Windows doivent encore être validées.

## Travaux récemment terminés

### P1 — capture et finalisation asynchrones

- L’acquisition des images de relecture et leur encodage JPEG s’exécutent dans une boucle en arrière-plan, et non sur le dispatcher WPF.
- L’export de la relecture utilise des entrées/sorties asynchrones et une exécution asynchrone de FFmpeg.
- L’arrêt et l’abandon d’un enregistrement attendent la fin des captures en cours sans bloquer l’interface ; l’arrêt audio et la finalisation des fichiers s’effectuent en arrière-plan.
- FFmpeg est lancé avec des arguments séparés. Ses flux de sortie sont lus de manière asynchrone et son processus ainsi que ses processus enfants peuvent être arrêtés. L’API prend en charge l’annulation et applique un délai maximal de trois minutes par défaut.
- Les événements des services qui mettent à jour WPF sont renvoyés vers le dispatcher de leur fenêtre. Les commandes d’enregistrement sont désactivées pendant la finalisation.

### P2 — mémoire et persistance

- L’encodeur de capture réutilise le bitmap capturé lorsqu’aucun redimensionnement n’est nécessaire. Les images redimensionnées utilisent un format de pixels 24 bits afin de réduire la mémoire des bitmaps intermédiaires.
- Les images de relecture sont gérées par un tampon dédié, limité à 160 Mio et purgé selon leur ancienneté. Les instantanés d’export réutilisent les références des images JPEG au lieu de copier leurs tableaux d’octets.
- Les paramètres, l’historique des sorties et l’historique du presse-papiers sont écrits dans des fichiers temporaires du même dossier, puis remplacés atomiquement.
- Les paramètres sont normalisés et bornés au chargement. Les fichiers JSON endommagés des paramètres et des historiques sont mis en quarantaine au lieu d’être écrasés silencieusement.
- Les services de persistance acceptent des chemins de fichiers explicites. Les tests peuvent ainsi utiliser des dossiers temporaires isolés sans toucher au profil utilisateur réel.

### P3 — fichiers générés, tests de régression et mesures de performance

- Suppression du fichier temporaire WPF généré par erreur et ajout de règles Git pour ignorer les projets temporaires WPF régénérés ainsi que les résultats de mesure des performances.
- Ajout de tests de régression pour la limite mémoire du tampon, la réutilisation des données JPEG, la sauvegarde et le chargement des paramètres, les valeurs invalides, la récupération après corruption et l’isolation de l’historique des sorties.
- Ajout d’un projet de mesure autonome pour la capture réelle de l’écran et l’encodage JPEG, ainsi que pour l’insertion d’images synthétiques dans le tampon de relecture. Il mesure les percentiles de latence, les allocations managées, la taille des données conservées et la mémoire du processus. L’export CSV est facultatif.
- La suite de tests Release compte actuellement 97 tests, dont le cycle de vie de l’enregistrement, la chronologie pause/reprise, la durée MP4 et le silence pendant les pauses, les tests matériels audio Windows et le mixage FFmpeg.

### Issue GitHub n° 1 — capture fiable du son système via WASAPI (implémentation terminée ; validation matérielle Windows 10 restante)

- Remplacement de la capture du son système basée sur un nom de périphérique par WASAPI loopback sur la sortie Multimedia Windows par défaut. Stereo Mix n’est pas nécessaire.
- Ajout de l’actualisation et du repli des périphériques, d’un arrêt sûr, de segments WAV lors des pauses/reprises et du signalement visible des pannes. Une panne audio rend la source indisponible sans arrêter la capture vidéo ni perdre la vidéo.
- Définition d’une chronologie commune : la première image vidéo définit le zéro ; les segments audio portent leurs décalages ; FFmpeg complète ou rogne les segments et mixe microphone et son système sans utiliser `-shortest` pour tronquer la vidéo.
- Ajout de tests matériels pour les modes microphone seul, son système seul et combiné, la sélection de la sortie par défaut et la pause/reprise WASAPI. Ajout de tests FFmpeg pour les trois modes et de tests d’isolation des pannes.
- Vérification sous Windows 11 Famille, build 26300 x64, avec la sortie Bose SLIII par défaut et le microphone Realtek. Une vérification matérielle Windows 10 reste à effectuer.
- Documentation des versions Windows ciblées, du matériel testé, de l’architecture et des étapes manuelles dans `AUDIO_TESTING.fr.md` (versions anglaise et chinoise simplifiée également disponibles).

### Issue GitHub n° 2 — stabilité et validation pause/reprise (couverture automatisée terminée ; longues sessions physiques restantes)

- Ajout de tests du cycle de vie pour les quatre modes audio, les décalages pause/reprise, l’exclusion du temps de pause, les enregistrements répétés, les dossiers de sortie uniques et le nettoyage après abandon.
- Ajout d’un test FFmpeg/FFprobe qui vérifie les flux vidéo/audio MP4, le décodage complet, une durée de trois secondes et un intervalle silencieux entre deux segments audio audibles.
- Ajout d’un test de capture continue de 15 secondes avec des charges synthétiques de 64 Kio stockées sur disque et de diagnostics par enregistrement : demandes, images ignorées, échecs, octets JPEG, mémoire de travail maximale et mémoire privée maximale.
- La fermeture de la fenêtre d’enregistrement active ou du HUD affiche désormais l’autre surface de contrôle au lieu de laisser un enregistrement actif invisible.
- Ajout de `Test-RecordingArtifacts.ps1` pour vérifier la lecture/le décodage MP4, la durée, les fichiers audio annexes et les séquences d’images ; ajout de `Measure-RecordingStability.ps1` pour mesurer mémoire, CPU, threads, handles et espace temporaire pendant les longues sessions.
- Documentation de la suite automatisée, de la matrice manuelle R01–R12, de l’environnement et des exigences Windows 10/11 dans `RECORDING_VALIDATION.fr.md` et ses versions anglaise et chinoise.

### Localisation et documentation

- Les messages d’état et les erreurs passent désormais par le catalogue de traduction, y compris les messages dynamiques liés à l’enregistrement, à la relecture, à l’audio, au presse-papiers et aux raccourcis.
- Les boîtes de dialogue des captures épinglées et les filtres de format d’image utilisent la langue sélectionnée.
- Les documentations anglaise, française et chinoise simplifiée sont séparées, avec une terminologie cohérente et sans mélange de langues sur une même ligne.

### Issue GitHub n° 4 — capture multi-écrans et haute résolution DPI (implémentation et géométrie automatisée terminées ; matrice physique DPI restante)

- Déclaration de Per-Monitor V2 dans le manifeste, en cohérence avec le paramètre WPF du projet.
- Refonte du sélecteur : une fenêtre native par écran physique. Le curseur et la sélection utilisent les pixels physiques du bureau ; chaque fenêtre effectue son rendu avec la transformation DPI de son écran.
- Correction des limites du bureau virtuel, y compris les origines négatives ; les zones sont arrondies vers l’extérieur, limitées au bureau virtuel et leurs dimensions réelles sont enregistrées.
- Placement du HUD, de la notification de capture et des captures épinglées sur l’écran du curseur, selon sa zone de travail et le DPI effectif de la fenêtre.
- Ajout de tests automatisés pour le bureau virtuel, les origines négatives, les bords fractionnaires, le découpage et le placement des fenêtres. Ajout d’un script de diagnostic et de matrices d’acceptation en anglais, français et chinois simplifié.
- L’environnement de développement détecte deux écrans 1920×1080 côte à côte à 100 %. Il s’agit uniquement d’un relevé de configuration ; les essais physiques à 125/150 %, DPI mixtes, dispositions verticales et Windows 10 restent à effectuer.
- L’application ne propose pas de zone d’enregistrement vidéo sélectionnable : l’enregistrement et la relecture capturent tout le bureau virtuel. Cette limite est documentée et n’est pas présentée comme une fonctionnalité testée.

## Travaux de validation restants

### 1. Enregistrement sur du matériel réel

- Exécuter les enregistrements GUI de 15 et 60 minutes sur du matériel Windows 11 et Windows 10 ; conserver le CSV mémoire et le rapport de validation des artefacts.
- Répéter la matrice avec les résolutions, profils, combinaisons microphone/son système et un second périphérique audio prévus.
- Valider le manque d’espace disque et les changements de périphérique sur du matériel réel ; le cycle de vie automatisé, la pause/reprise, la fermeture des fenêtres, le nettoyage des sessions répétées et le silence audio synthétique sont déjà testés.

### 2. Relecture instantanée sur du matériel réel

- Vérifier que chaque durée proposée produit la couverture et la durée de lecture attendues.
- Comparer la qualité d’image et la fluidité des profils de qualité.
- Mesurer les pertes d’images et l’utilisation mémoire sous forte charge.
- Tester l’export d’une relecture pendant un enregistrement classique.
- Valider l’annulation et les délais maximaux, notamment la conservation des images si l’encodage MP4 échoue.
- Exécuter les tests de performance sur plusieurs résolutions d’écran et comparer les résultats à une référence enregistrée.

### 3. Audio et périphériques

- Valider le matériel Windows 10 ainsi que d’autres périphériques de sortie/entrée, fréquences d’échantillonnage et changements de périphérique pendant une session.
- Mesurer la dérive audio/vidéo sur de longues sessions et vérifier la synchronisation après pause/reprise sur plusieurs configurations matérielles.
- Vérifier la finalisation et le nettoyage des fichiers audio après un arrêt normal, un abandon et la fermeture de l’application.
- Terminer la matrice physique du ticket n° 4 : 125/150 %, DPI/résolutions mixtes, dispositions verticales, changements d’écran et Windows 10 ; la géométrie automatisée et les dimensions des captures sont couvertes.

### 4. Récupération des fichiers et expérience utilisateur

- Documenter l’organisation finale des dossiers de captures, d’enregistrements et de relectures.
- Décider si les fichiers WAV du microphone et du son système doivent être conservés après un export MP4 réussi.
- Définir la récupération en cas d’échec de finalisation ou de disque plein.
- Ajouter une progression visible et une annulation pour les exports longs lorsque cela est pertinent.

### 5. Architecture, intégration continue et publication

- Réduire progressivement les responsabilités de `WindowFlowCoordinator` à mesure que les limites entre services se précisent.
- Ajouter des tests de bout en bout pour la capture, l’audio, la relecture et la fermeture de l’application.
- Ajouter une validation CI pour la compilation Release, les tests et la compilation de l’outil de mesure des performances.
- Finaliser le package MSIX et automatiser la création de packages signés ainsi que les publications GitHub.

## Prochaine étape recommandée

Effectuer la validation restante sur Windows 10 et avec un enregistrement prolongé : activer le microphone et le son système, tester la pause/reprise et les changements de périphérique, examiner le MP4 et les WAV annexes, puis répéter le scénario pour vérifier le nettoyage. Conserver les résultats des tests de performance comme référence afin de comparer objectivement les prochaines optimisations.