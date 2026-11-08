# Normes visuelles de ScreenTools

**Documentation :** [English](DESIGN.md) · [Français](DESIGN.fr.md) · [简体中文](DESIGN.zh-CN.md)

Ce document définit les règles d’interface à appliquer dans ScreenTools. L’objectif est de créer un outil de bureau sobre, soigné et proche des conventions natives de Windows, et non un tableau de bord composé de panneaux sans cohérence.

## 1. Orientation visuelle

Le principe directeur est celui du **conservateur numérique** : l’interface doit s’effacer pour laisser la place au contenu et à la tâche en cours. Privilégiez une asymétrie maîtrisée, des espaces généreux et des groupes clairement définis plutôt que des grilles denses et des décorations superflues.

Le produit doit être :

- **Sobre :** aucune couleur, aucun effet ni indicateur d’état sans utilité précise.
- **Précis :** alignements, espacements, dimensions des icônes et hiérarchie typographique cohérents.
- **Léger :** les fenêtres doivent ressembler à des outils natifs du bureau, pas à de lourds conteneurs applicatifs.
- **Efficace :** les actions fréquentes sont mises en avant ; les réglages occasionnels restent secondaires.

## 2. Couleurs et surfaces

- Les gris neutres constituent la base. Évitez le noir pur (`#000000`) et les couleurs décoratives très saturées.
- `Primary` (`#005EB1`) est réservé au focus, à la sélection et aux actions principales. Sa variante active est `#00529B`.
- Couleurs de texte : `Text-primary` `#2D3435`, `Text-secondary` `#4F5658`, `Icon-secondary` `#5F6B7A`.
- Surfaces : `Surface-page` `#F9F9F9`, `Surface-card` `#F2F4F4`, `Surface-chip` `#EEF1F2`, `Surface-glass` `#F0FFFFFF`, `Surface-panel-glass` `#F2FFFFFF`.
- Construisez la hiérarchie avec de légères différences de surface. Préférez l’espacement et le contraste doux aux séparateurs rigides.
- Lorsqu’une bordure est nécessaire pour l’accessibilité, utilisez `outline-variant` avec une opacité d’environ 10 à 20 %.
- Réservez le rouge aux indicateurs d’état importants. En règle générale, un écran ne doit pas utiliser plus d’une couleur d’accent principale et d’une couleur d’alerte.

## 3. Matières, profondeur et ombres

Respectez une structure à trois couches :

1. **Couche hôte :** transparente ou presque ; elle ne porte pas de contenu et ne doit pas devenir un grand panneau opaque.
2. **Couche fonctionnelle flottante :** elle contient les commandes et les informations ; utilisez des surfaces blanches translucides ou des tons neutres très clairs.
3. **Couche d’accentuation :** réservée à la sélection, à l’action principale, à un commutateur actif ou à une indication essentielle.

N’utilisez Mica ou un effet de verre discret que si cela renforce l’intégration au bureau Windows. Conservez une opacité d’environ 80 à 90 % et évitez le flou excessif. Toutes les couches flottantes doivent partager un seul style d’ombre ambiante : couleur `on-surface` faiblement opaque, flou d’environ 20 à 28 px et profondeur de 4 à 8 px. Évitez les ombres dures, sombres ou ternes.

## 4. Typographie

Utilisez Segoe UI pour les textes latins et une police CJK système adaptée pour le chinois. Respectez la hiérarchie suivante :

| Rôle | Taille | Utilisation |
|---|---:|---|
| Display-LG | 3,5 rem | Message d’accueil ou chiffre important, rarement |
| Headline-SM | 1,5 rem | Titres des fonctions principales |
| Title-MD | 1,125 rem | Titres de groupes et de paramètres |
| Body-MD | 0,875 rem | Texte courant et champs de saisie |
| Label-SM | 0,6875 rem | Notes et métadonnées |

Sur la page des paramètres, visez un titre de page de 24 px semibold, des titres de groupe de 16 px medium, des intitulés de réglage de 16 px regular, des descriptions de 15 à 16 px et des raccourcis monospaces de 12 px. Dans les HUD flottants, le minuteur doit mesurer environ 56 à 58 px en semibold ; les libellés restent compacts, entre 10 et 14 px.

Les titres doivent être plus marqués que les descriptions. Gardez une interligne confortable et rendez les durées, les minuteries et les résolutions faciles à repérer.

## 5. Composants et interactions

### Boutons

- Principal : fond `primary`, texte `on-primary` et rayon de 4 px.
- Secondaire : surface `secondary-container`, sans bordure décorative.
- Survol : changement de fond discret. Pression : légère réduction autour de `0.98`.

### Champs de saisie

Utilisez soit un soulignement discret de 2 px en `outline-variant`, soit une surface remplie `surface-container-high`. Au focus, appliquez l’accent principal et éclaircissez légèrement la surface.

### Listes et cartes

Évitez les mises en page saturées de séparateurs. Distinguez les éléments par un espacement vertical régulier (environ `spacing.4`, soit 0,9 rem). Un élément sélectionné peut utiliser `surface-container-highest` avec un indicateur principal de 3 px à gauche.

### Animations

Les transitions doivent être courtes et utiles ; évitez celles qui dépassent 200 ms. Préférez une courbe de décélération nette, par exemple `cubic-bezier(0, 0, 0, 1)`.

## 6. Icônes, formes et mise en page

- Utilisez des icônes monochromes en contour, avec une épaisseur cohérente. Le gris secondaire est la valeur par défaut ; le bleu principal est réservé aux états actifs.
- Rayons : 4 px pour les petits boutons et puces, 8 px pour les cartes moyennes, 12 à 16 px pour les couches flottantes et barres d’outils, et 16 px pour les grandes cartes principales. Ne dépassez pas 16 px, sauf pour les commutateurs, badges et boutons circulaires.
- Laissez davantage d’espace entre les groupes et resserrez les espacements à l’intérieur d’un groupe.
- Chaque fenêtre ne doit avoir qu’un seul point focal : le contenu des paramètres, le minuteur d’enregistrement ou l’action principale du HUD de configuration.

## 7. Règles propres à ScreenTools

### Page des paramètres

- Recherchez une interface calme et fiable, confortable lors d’une utilisation prolongée.
- Ne transformez pas la page en tableau de bord ou en page marketing.
- Réservez les raccourcis globaux aux captures d’écran, à l’enregistrement et à la relecture instantanée. Les réglages moins fréquents — dossier de sortie, démarrage automatique, préférences audio et qualité — restent dans les paramètres.

### HUD d’enregistrement

- Il doit ressembler à un petit outil flottant, pas à une fenêtre d’application autonome.
- Gardez l’arrière-plan et les informations visuellement légers.
- Le mode actuel doit être immédiatement identifiable ; les outils secondaires doivent rester discrets.
- N’encombrez pas la barre d’outils avec des icônes.

### Superposition d’enregistrement actif

- Le minuteur constitue le point focal.
- Les indicateurs audio donnent un retour sans concurrencer le minuteur.
- Les commandes pause et arrêt doivent être immédiatement accessibles sans alourdir la fenêtre.
- Une commande secondaire ou « plus d’actions » peut exister, mais doit rester discrète.

## 8. Motifs à éviter et ordre de mise en œuvre

Évitez l’empilement de panneaux gris, plusieurs systèmes d’ombres concurrents, une accumulation de surfaces bleues, les bordures sans fonction, les raccourcis pour chaque fonctionnalité et les styles d’icônes incohérents.

Appliquez les changements visuels dans cet ordre :

1. Mise en page et espaces.
2. Hiérarchie du texte.
3. Couleurs et matières.
4. Icônes et ombres.

L’interface doit faciliter le travail, puis s’effacer. Le meilleur design est celui que l’on remarque à peine.