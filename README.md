# R.E.T (Réservation Équerres / Tins)
### Stage de 2ème année BTS SIO

## STX France
  STX est une entreprise spécialisée dans la construction de structures flottantes (paquebots, sous-stations offshores ou encore navires de guerre). En mars 2018, elle inaugurait notamment le *Symphony of the Seas*, le plus grand paquebot du monde. Pour arriver à de tels résultats, STX France dispose de plusieurs chantiers situés près du port de Saint-Nazaire. L'entreprise voit de nombreuses personnes travailler sur ses chantiers (de 2 500 à 6 000 lorsque la demande est importante) et elle ne possède pas moins d'outillages.Ceux-ci sont utilisés puis sont déplacés sur un autre chantier et ainsi de suite. Leur traçabilité est donc essentielle.
J'ai, pendant mon stage, intégré le bureau d'étude du service coque métallique. Celui-ci est divisé en plusieurs secteurs parmi lesquels le secteur "méthodes logistiques", dont j'ai fait partie.


### Construction d'un navire
La construction de navires suit un processus bien établi qui ne varie que dans de rares cas (navires militaires par exemple). Si la construction d'un navire demande beaucoup de travail et de précision, que ce soit dans les bureaux ou sur les chantiers, on peut toutefois schématiser celle-ci de manière assez simple.

Un **panneau** peut être schématisé par un morceau de tôle souvent long d'une vingtaine de mètres. Il est modélisé en 3D puis envoyé à l'usinage. Celui-ci sera volontairement construit à l'envers de manière à simplifier la pose des câbles électriques et autres tuyaux. Lorsque l'installation de ceux-ci est finie, le panneau est dit "armé" et il faut alors le retourner dans le bon sens.
Les panneaux dans le bon sens sont alors empilés les uns sur les autres, ils forment alors ce qu'on appelle un **bloc**. Ces blocs, à leur tour, vont être empilés les uns sur les autres. C'est cet ensemble de blocs qui constitue le navire.

### Utilisation des équerres
Lorsque des blocs ou des équerres sont empilés sur un chantier, ceux-ci doivent être maintenus par des équerres. Celles-ci ont une certaine hauteur, parfois réglable, et peuvent supporter une certaine charge.
C'est au groupe méthodes logistiques d'attribuer les équerres à un chantier en fonction de leurs caractéristiques.

#### Analyse de l'existant
La réservation des équerres se faisait jusqu'à maintenant via un fichier Excel, répertoriant toutes les équerres existantes et où il fallait colorer la ligne de l'équerre à réserver sur une certaine période.
L'objectif durant ce stage sera de développer un logiciel permettant à l'équipe méthodes logistiques de réserver facilement des équerres.
De nombreuses documentations concernant les propriétés des équerres m'ont été remises. Elles m'ont permis de créer la base de donnée nécessaire pour utiliser le logiciel.
![Schema Base De Donnees](https://github.com/VirgileJallonPeriaux/R.E.T/blob/master/BaseDeDonnees/schemaWorkbenchBDD.png)

## Cahier des charges du logiciel : quelques points clefs
La réservation des équerres est une tâche chronophage. Le principal objectif du logiciel est de gagner du temps; ou plutôt de ne pas en perdre car la réservation des équerres via le fichier Excel est laborieuse et source d'erreurs.
- Simplifier la recherche des équerres disponibles<br>
Le logiciel devra proposer à l'utilisateur une liste des équerres disponibles entre 2 dates déterminées. Celles-ci devront répondre aux caractéristiques exigées (hauteur + charge supportée).
- Générer des statistiques<br>
Certains types d'équerres sont beaucoup plus utilisés que d'autres. Il peut arriver que plus aucune équerre correspondante ne soit disponible. Pour ne pas réitérer cette situation plus que dérangeante, le service coque métallique envisage l'achat de nouvelles équerres. Celles-ci ayant un coût élevé, la direction de STX demande des statistiques sur l'utilisation de ces équerres. Les statistiques établies permettront de démontrer l'intérêt réel d'un tel investissement.
- Gérer plusieurs utilisateurs<br>
Le logiciel sera principalement utilisé par les employés du secteur méthodes logistiques. Ceux-ci doivent, à tout moment, pouvoir consulter les réservations des équerres.<br>
- Gérer les droits des utilisateurs<br>
Tous les utilisateurs n'ont pas les même droits.<br>
Exemple :
  - Les employés du secteur méthodes logistiques peuvent réserver, annuler une réservation, créer de nouvelles équerres... Ils ne voient que les types des équerres et non les repères servant à les identifier.
  - Les employés du secteur "pré montage" en revanche, n'ont pas le droit à la modification. Ils peuvent cependant voir les repères des équerres.

## Le logiciel
Le logiciel sera développé en C# pour plusieurs raisons : 
- Le logiciel est assez complet, il prendra donc du temps à réaliser. Le programmer dans un langage que je ne connais que peu ne me permettrait pas de réaliser le cahier des charges convenu.
- Le logiciel ne sera certainement pas fini avant la fin de mon stage. Deux solutions s'offrent alors : soit un autre stagiaire prends le relais soit c'est un membre de l'équipe méthodes logistiques, Matthieu Michel (dit "MattMich"), qui s'en chargera. Il est le plus susceptible d'intervenir sur le code si une erreur se produit. Ayant quelques notions de Visual Basic (langage assez proche du C# dans sa syntaxe), il sera à même de modifier le code du logiciel d'autant plus que celui-ci a déjà eu l'occasion d'utiliser l'EDI Visual Studio.

Du schéma de base de données découle ce diagramme de classes
![diagClasse](https://github.com/VirgileJallonPeriaux/R.E.T/blob/master/Documentation/Logiciel/diagClasseRet.PNG)
Ou du moins c'est à ça qu'il était censé ressembler si il n'y avait pas quelques contraintes à prendre en compte...

## Optimisation de la mémoire
Dans le contexte qu'est celui de la gestion des équerres, il y a beaucoup de données à traiter.
Pour améliorer la vitesse des traitements, il a fallu, en quelques sortes, ruser.
Ainsi la classe *Reservation* (en orange dans le diagramme) n'existe pas.
Il semble pourtant nécessaire d'associer à un bloc zéro à plusieurs équerres réservées pour celui-ci.
Seulement, pour instancier et valoriser toutes les données membres d'une équerre, il faut :<br>
  - Instancier une équerre<br>
  - Instancier un type d'équerre<br>
  - Instancier une liste de propriétés<br>
  - Pour chaque propriété, il faut instancier un moyen de transport qui valorisera une donnée membre de la propriété<br>
  - Chaque propriété valorisera alors la liste des propriétés d'un type d'équerre<br>
  - Enfin, le type d'équerre valorisera une donnée membre de l'équerre<br>
  
Instancier une équerre, c'est donc garder en mémoire beaucoup de données. Il n'y a rarement que une seule équerre réservée sur un bloc. On peut supposer répéter cette opération jusqu'à 20x par bloc...
Il est donc inconcevable de charger en mémoire, au démarrage de l'application par exemple, toutes les réservations associées à chaque bloc. Actuellement 4453...
  
#### Dans la fenêtre listant les équerres réervées :
Pour optimiser l'utilisation de la mémoire dans cette situation, on ne va pas créer de variable de type Equerre.
De plus, seules quelques informations que peut nous fournir un objet de type Equerre nous sont utiles dans cette fenêtre. 
On préférera alors recueillir les informations nécessaire dans les liste de string puis les afficher.

#### Dans la fenêtre representant la fiche technique d'une équerre :
Cette situation est différente de la précédente. En effet, la fiche technique d'une équerre doit être complète. Nous avons besoin de toutes les données que peut nous fournir un objet de type Equerre.
Nous instancierons alors l'équerre en mémoire.
*Nb* : Les seules fois où il y a une instanciation d'équerre en mémoire, c'est lorsqu'il n'y a qu'une seule équerre à charger.

### Serveur SQL
Le SGBDR MySQL sera utilisé pour la base de données. STX possède Access mais le service informatique est assez réticent à l'idée de donner des accès à quelqu'un qui n'appartient pas à leur service. De même, STX possède des serveurs. Pour pouvoir y stocker une base de données, il faut faire une demande écrite. Les accès ne sont donnés que tous les 3 mois. Lorsque mon stage se termine, l'équipe méthodes logistiques n'a toujours pas accès au serveur SQL.
