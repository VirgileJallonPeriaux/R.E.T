-- source C:/Users/lfm/Desktop/scriptCreationBDD3.sql

drop database if exists ret2;
create database ret2;
use ret2;


-- ---------------------------------------------- --
-- ------------ TABLES INDÉPENDANTES ------------ --
-- ---------------------------------------------- --
create table Utilisateur
(
	id int not null auto_increment,
	trigramme varchar(3) not null unique,
	nom varchar(50) default "",
	prenom varchar(50) default "",
	rang tinyint not null,
	main tinyint,
	primary key(id)
);

create table PersistanceParametres
(
	id int not null,
	cheminDossierSauvegardePdf varchar(280),
	cheminFichierTxtConnexionBdd varchar(280),
	cheminFichierExtractionC212P varchar(280),
	cheminImageErreur varchar(280),
	dateMiseAJour date default '0001-01-01',
	primary key(id)
);

-- ---------------------------------------------- --
-- ------------------- TINS --------------------- --
-- ---------------------------------------------- --
create table Outillage
(
	id int not null auto_increment,
	libelle varchar(50) not null,
	primary key(id)
);

create table HistoriqueOutillage
(
	id int not null auto_increment,
	idOutillage int not null,
	nombre int,
	dateMiseAJour datetime,
	primary key(id)
);

-- ---------------------------------------------- --
-- ------------------ ÉQUERRES ------------------ --
-- ---------------------------------------------- --
create table TypeEquerre
(
	id int not null auto_increment,
	repere varchar(10) not null,
	numeroPlan varchar(25),
	semblable tinyint,
	reglageHauteur tinyint,
	cheminImage varchar(100) default "",
	primary key(id)
);

create table Equerre
(
	id int not null auto_increment,
	idTypeEquerre int not null,
	repere varchar(10) unique,
	remarque varchar(280) default "",
	primary key(id)
);

create table Propriete
(
	id int not null auto_increment,
	idTypeEquerre int not null,
	hauteur int,
	charge int,
	primary key(id)
);

-- ---------------------------------------------- --
-- ------------------- BLOCS -------------------- --
-- ---------------------------------------------- --
create table Navire
(
	id int not null auto_increment,
	nom varchar(3) unique,
	primary key(id)
);

create table Bloc
(
	id int not null auto_increment,
	idNavire int not null,
	repere varchar(10),
	dateDebutPm date,
	dateFinPm date,
	dateFinBord date,
	dateDebutPmVerrouillee tinyint,
	dateFinPmVerrouillee tinyint,
	remarque varchar(280),
	stadeEtudePm tinyint default 0,
	stadeEtudeBord tinyint default 0,
	primary key(id)
);

-- ---------------------------------------------- --
-- --------------- RÉSERVATIONS ----------------- --
-- ---------------------------------------------- --
create table ReserverEquerre
(

	id int not null auto_increment,
	idBloc int not null,
	idEquerre int not null,	
	idPropriete int not null,
	pm tinyint not null,
	primary key(id)
);

create table ReserverOutillage
(
	id int not null auto_increment,
	idOutillage int not null,
	idBloc int not null,
	nombre int not null,
	pm tinyint,
	primary key(id)
);

create table Pret
(
	id int not null auto_increment,
	idEquerre int not null,
	dateDebut date,
	dateFin date,
	remarque varchar(280),
	idPropriete int not null,
	idBloc int default null,		-- Clé Étrangère Optionnelle (un prêt n'est pas toujours associé à un bloc)
	pm tinyint not null,
	primary key(id)
);

create table TravauxEquerre
(
	id int not null auto_increment,
	idEquerre int not null,
	dateDebut date,
	dateFin date,
	remarque varchar(280),
	primary key(id)
);

-- ---------------------------------------------- --
-- ----------------- TRANSPORTS ----------------- --
-- ---------------------------------------------- --
create table Transport
(
	id int not null auto_increment,
	classe varchar(1),
	libelle varchar(50) not null unique,
	primary key(id)
);

create table Deplacer
(
	id int not null auto_increment,
	idPropriete int not null,
	idTransport int not null,
	primary key(id)
);

-- ---------------------------------------------- --
-- ------------------ AUTRES -------------------- --
-- ---------------------------------------------- --
create table EtatMiseAJourPdf
(
	id int not null auto_increment,
	idBloc int not null,
	pm tinyint default 0,
	bord tinyint default 0,
	primary key(id)
);

-- ---------------------------------------------- --
-- ------- CONTRAINTES DE CLEFS ÉTRANGÈRES ------ --
-- ---------------------------------------------- --
alter table HistoriqueOutillage
add constraint fk_HistoriqueOutillage_idOutillage_Outillage
foreign key(idOutillage)
references Outillage(id);

alter table Equerre
add constraint fk_Equerre_idTypeEquerre_TypeEquerre
foreign key(idTypeEquerre)
references TypeEquerre(id);

alter table Propriete
add constraint fk_Propriete_idTypeEquerre_TypeEquerre
foreign key(idTypeEquerre)
references TypeEquerre(id);

alter table Bloc
add constraint fk_Bloc_idNavire_Navire
foreign key(idNavire)
references Navire(id);

alter table ReserverEquerre
add constraint fk_ReserverEquerre_idBloc_Bloc
foreign key(idBloc)
references Bloc(id);

alter table ReserverEquerre
add constraint fk_ReserverEquerre_idEquerre_Equerre
foreign key(idEquerre)
references Equerre(id);

alter table ReserverEquerre
add constraint fk_ReserverEquerre_idPropriete_Propriete
foreign key(idPropriete)
references Propriete(id);

alter table ReserverOutillage
add constraint fk_ReserverOutillage_idOutillage_Outillage
foreign key(idOutillage)
references Outillage(id);

alter table ReserverOutillage
add constraint fk_ReserverOutillage_idBloc_Bloc
foreign key(idBloc)
references Bloc(id);

alter table Pret
add constraint fk_Pret_idEquerre_Equerre
foreign key(idEquerre)
references Equerre(id);

alter table Pret
add constraint fk_Pret_idPropriete_Bloc
foreign key(idPropriete)
references Propriete(id);

alter table Pret
add constraint fk_Pret_idBloc_Bloc
foreign key(idBloc)
references Bloc(id);

alter table TravauxEquerre
add constraint fk_TravauxEquerre_idEquerre_Equerre
foreign key(idEquerre)
references Equerre(id);

alter table Deplacer
add constraint fk_Deplacer_idPropriete_Propriete 
foreign key(idPropriete)
references Propriete(id);

alter table Deplacer
add constraint fk_Deplacer_idTransport_Transport
foreign key(idTransport)
references Transport(id);

alter table EtatMiseAJourPdf
add constraint fk_EtatMiseAJourPdf_idBloc_Bloc
foreign key(idBloc)
references Bloc(id);

-- ---------------------------------------------- --
-- ----------------- TRIGGER(S) ----------------- --
-- ---------------------------------------------- --

-- Chaque fois qu'un nouveau bloc est créé, on ajoute son id dans la table EtatMiseAJourPdf
delimiter |

CREATE TRIGGER trigger_After_Insert_Bloc after INSERT
ON Bloc FOR EACH ROW
BEGIN
    INSERT INTO EtatMiseAJourPdf(idBloc) values (new.id);
END|

delimiter ;

source C:\Users\vivij\Desktop\Dossier_RET\BaseDeDonnees\scriptInsertionBDD3.sql
