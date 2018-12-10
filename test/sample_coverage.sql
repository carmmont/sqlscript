
--drop table #tempresult
--drop table #tmpNumSeq
--drop table #filter

CREATE TABLE #tmpNumSeq(
DataIns datetime,
IdGestTurni int,
IdTeam int,
IdTurno int,
IdUte int,
NumSeq int)

CREATE TABLE #tempResult(
Cis nvarchar,
CodOP nvarchar,
CodStz nvarchar,

data nvarchar,
Esito smallint,
idcda int,
IdCis int,
Idoperazione int,
idStazione int,
Modello char)

CREATE TABLE #filter(
Codice nvarchar,
dis nvarchar,
Idbond nvarchar)

CREATE TABLE #caratteristicheDis(
cis nvarchar,

disegno char,
frn nvarchar,
motiv varchar,
Qta decimal)

exec [dbo].[Async_st_CliInsertAnomalieDelibera_ext]

--exec [dbo].[st_SvInsertTransiti]

--exec [dbo].[OTT_SendQualityTransit] 171, '7495333'