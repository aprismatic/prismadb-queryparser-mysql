/*
MySQL (Positive Technologies) grammar
The MIT License (MIT).
Copyright (c) 2015-2017, Ivan Kochurkin (kvanttt@gmail.com), Positive Technologies.
Copyright (c) 2017, Ivan Khudyashev (IHudyashov@ptsecurity.com)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

parser grammar AntlrMySqlParser;

options { tokenVocab=AntlrMySqlLexer; }


// Top Level Description

root
    : sqlStatements? MINUSMINUS? EOF
    ;

sqlStatements
    : (sqlStatement MINUSMINUS? SEMI | emptyStatement)*
    (sqlStatement (MINUSMINUS? SEMI)? | emptyStatement)
    ;

sqlStatement
    : ddlStatement | dmlStatement
    ;

emptyStatement
    : SEMI
    ;

ddlStatement
    : createTable | alterTable | dropTable
    ;

dmlStatement
    : selectStatement | insertStatement | updateStatement
    | deleteStatement
    ;


// Data Definition Language

//    Create statements

createTable
    : CREATE TABLE
      tableName
      createDefinitions
    ;

// details

createDefinitions
    : '(' createDefinition (',' createDefinition)* ')'
    ;

createDefinition
    : uid columnDefinition                                          #columnDeclaration
    ;

columnDefinition
    : dataType columnConstraint*
    ;

columnConstraint
    : nullNotnull                                                   #nullColumnConstraint
    | DEFAULT defaultValue                                          #defaultColumnConstraint
    | AUTO_INCREMENT                                                #autoIncrementColumnConstraint
    | PRIMARY? KEY                                                  #primaryKeyColumnConstraint
    ;

//    Alter statements

alterTable
    : ALTER TABLE
      tableName
      alterSpecification
    ;

// details

alterSpecification
    : MODIFY COLUMN?
      uid columnDefinition                                          #alterByModifyColumn
    ;


//    Drop statements

dropTable
    : DROP TABLE
      tables
    ;


// Data Manipulation Language

//    Primary DML Statements


deleteStatement
    : singleDeleteStatement
    ;

insertStatement
    : INSERT
      INTO? tableName
      (
        ('(' columns=uidList ')')? insertStatementValue
      )
    ;

selectStatement
    : querySpecification                                            #simpleSelect
    ;

updateStatement
    : singleUpdateStatement
    ;

// details

insertStatementValue
    : insertFormat=(VALUES | VALUE)
      '(' expressionsWithDefaults ')'
        (',' '(' expressionsWithDefaults ')')*
    ;

updatedElement
    : fullColumnName '=' (expression | DEFAULT)
    ;

assignmentField
    : uid | LOCAL_ID
    ;

//    Detailed DML Statements

singleDeleteStatement
    : DELETE
    FROM tableName
      (WHERE expression)?
    ;

singleUpdateStatement
    : UPDATE tableName
      SET updatedElement (',' updatedElement)*
      (WHERE expression)?
    ;

// details

orderByClause
    : ORDER BY orderByExpression (',' orderByExpression)*
    ;

orderByExpression
    : expression order=(ASC | DESC)?
    ;

tableSources
    : tableSource (',' tableSource)*
    ;

tableSource
    : tableSourceItem joinPart*                                     #tableSourceBase
    | '(' tableSourceItem joinPart* ')'                             #tableSourceNested
    ;

tableSourceItem
    : tableName
      (PARTITION '(' uidList ')' )? (AS? alias=uid)?
      (indexHint (',' indexHint)* )?                                #atomTableItem
    | (
      selectStatement
      | '(' parenthesisSubquery=selectStatement ')'
      )
      AS? alias=uid                                                 #subqueryTableItem
    | '(' tableSources ')'                                          #tableSourcesItem
    ;

indexHint
    : indexHintAction=(USE | IGNORE | FORCE)
      keyFormat=(INDEX|KEY) ( FOR indexHintType)?
      '(' uidList ')'
    ;

indexHintType
    : JOIN | ORDER BY | GROUP BY
    ;

joinPart
    : (INNER | CROSS)? JOIN tableSourceItem
      (
        ON expression
        | USING '(' uidList ')'
      )?                                                            #innerJoin
    | STRAIGHT_JOIN tableSourceItem (ON expression)?                #straightJoin
    | (LEFT | RIGHT) OUTER? JOIN tableSourceItem
        (
          ON expression
          | USING '(' uidList ')'
        )                                                           #outerJoin
    | NATURAL ((LEFT | RIGHT) OUTER?)? JOIN tableSourceItem         #naturalJoin
    ;

//    Select Statement's Details

queryExpression
    : '(' querySpecification ')'
    | '(' queryExpression ')'
    ;

queryExpressionNointo
    : '(' querySpecificationNointo ')'
    | '(' queryExpressionNointo ')'
    ;

querySpecification
    : SELECT selectSpec* selectElements selectIntoExpression?
      fromClause? orderByClause? limitClause?
    | SELECT selectSpec* selectElements
    fromClause? orderByClause? limitClause? selectIntoExpression?
    ;

querySpecificationNointo
    : SELECT selectSpec* selectElements
      fromClause? orderByClause? limitClause?
    ;

unionParenthesis
    : UNION unionType=(ALL | DISTINCT)? queryExpressionNointo
    ;

unionStatement
    : UNION unionType=(ALL | DISTINCT)?
      (querySpecificationNointo | queryExpressionNointo)
    ;

// details

selectSpec
    : (ALL | DISTINCT | DISTINCTROW)
    | HIGH_PRIORITY | STRAIGHT_JOIN | SQL_SMALL_RESULT
    | SQL_BIG_RESULT | SQL_BUFFER_RESULT
    | (SQL_CACHE | SQL_NO_CACHE)
    | SQL_CALC_FOUND_ROWS
    ;

selectElements
    : (star='*' | selectElement ) (',' selectElement)*
    ;

selectElement
    : fullId '.' '*'                                                #selectStarElement
    | fullColumnName (AS? uid)?                                     #selectColumnElement
    | functionCall (AS? uid)?                                       #selectFunctionElement
    | (LOCAL_ID VAR_ASSIGN)? expression (AS? uid)?                  #selectExpressionElement
    ;

selectIntoExpression
    : INTO assignmentField (',' assignmentField )*                  #selectIntoVariables
    | INTO DUMPFILE STRING_LITERAL                                  #selectIntoDumpFile
    | (
        INTO OUTFILE filename=STRING_LITERAL
        (CHARACTER SET charset=charsetName)?
        (
          fieldsFormat=(FIELDS | COLUMNS)
          selectFieldsInto+
        )?
        (
          LINES selectLinesInto+
        )?
      )                                                             #selectIntoTextFile
    ;

selectFieldsInto
    : TERMINATED BY terminationField=STRING_LITERAL
    | OPTIONALLY? ENCLOSED BY enclosion=STRING_LITERAL
    | ESCAPED BY escaping=STRING_LITERAL
    ;

selectLinesInto
    : STARTING BY starting=STRING_LITERAL
    | TERMINATED BY terminationLine=STRING_LITERAL
    ;

fromClause
    : FROM tableSources
      (WHERE whereExpr=expression)?
      (
        GROUP BY
        groupByItem (',' groupByItem)*
        (WITH ROLLUP)?
      )?
      (HAVING havingExpr=expression)?
    ;

groupByItem
    : expression order=(ASC | DESC)?
    ;

limitClause
    : LIMIT
    (
      (offset=decimalLiteral ',')? limit=decimalLiteral
      | limit=decimalLiteral OFFSET offset=decimalLiteral
    )
    ;


// Transaction's Statements

startTransaction
    : START TRANSACTION (transactionMode (',' transactionMode)* )?
    ;

beginWork
    : BEGIN WORK?
    ;

commitWork
    : COMMIT WORK?
      (AND nochain=NO? CHAIN)?
      (norelease=NO? RELEASE)?
    ;

rollbackWork
    : ROLLBACK WORK?
      (AND nochain=NO? CHAIN)?
      (norelease=NO? RELEASE)?
    ;

savepointStatement
    : SAVEPOINT uid
    ;

rollbackStatement
    : ROLLBACK WORK? TO SAVEPOINT? uid
    ;

releaseStatement
    : RELEASE SAVEPOINT uid
    ;

lockTables
    : LOCK TABLES lockTableElement (',' lockTableElement)*
    ;

unlockTables
    : UNLOCK TABLES
    ;


// details

setAutocommitStatement
    : SET AUTOCOMMIT '=' autocommitValue=('0' | '1')
    ;

setTransactionStatement
    : SET transactionContext=(GLOBAL | SESSION)? TRANSACTION
      transactionOption (',' transactionOption)*
    ;

transactionMode
    : WITH CONSISTENT SNAPSHOT
    | READ WRITE
    | READ ONLY
    ;

lockTableElement
    : tableName (AS? uid)? lockAction
    ;

lockAction
    : READ LOCAL? | LOW_PRIORITY? WRITE
    ;

transactionOption
    : ISOLATION LEVEL transactionLevel
    | READ WRITE
    | READ ONLY
    ;

transactionLevel
    : REPEATABLE READ
    | READ COMMITTED
    | READ UNCOMMITTED
    | SERIALIZABLE
    ;


// Replication's Statements

//    Base Replication

changeMaster
    : CHANGE MASTER TO
      masterOption (',' masterOption)* channelOption?
    ;

changeReplicationFilter
    : CHANGE REPLICATION FILTER
      replicationFilter (',' replicationFilter)*
    ;

purgeBinaryLogs
    : PURGE purgeFormat=(BINARY | MASTER) LOGS
       (
           TO fileName=STRING_LITERAL
           | BEFORE timeValue=STRING_LITERAL
       )
    ;

resetMaster
    : RESET MASTER
    ;

resetSlave
    : RESET SLAVE ALL? channelOption?
    ;

startSlave
    : START SLAVE (threadType (',' threadType)*)?
      (UNTIL untilOption)?
      connectionOption* channelOption?
    ;

stopSlave
    : STOP SLAVE (threadType (',' threadType)*)?
    ;

startGroupReplication
    : START GROUP_REPLICATION
    ;

stopGroupReplication
    : STOP GROUP_REPLICATION
    ;

// details

masterOption
    : stringMasterOption '=' STRING_LITERAL                         #masterStringOption
    | decimalMasterOption '=' decimalLiteral                        #masterDecimalOption
    | boolMasterOption '=' boolVal=('0' | '1')                      #masterBoolOption
    | MASTER_HEARTBEAT_PERIOD '=' REAL_LITERAL                      #masterRealOption
    | IGNORE_SERVER_IDS '=' '(' (uid (',' uid)*)? ')'               #masterUidListOption
    ;

stringMasterOption
    : MASTER_BIND | MASTER_HOST | MASTER_USER | MASTER_PASSWORD
    | MASTER_LOG_FILE | RELAY_LOG_FILE | MASTER_SSL_CA
    | MASTER_SSL_CAPATH | MASTER_SSL_CERT | MASTER_SSL_CRL
    | MASTER_SSL_CRLPATH | MASTER_SSL_KEY | MASTER_SSL_CIPHER
    | MASTER_TLS_VERSION
    ;
decimalMasterOption
    : MASTER_PORT | MASTER_CONNECT_RETRY | MASTER_RETRY_COUNT
    | MASTER_DELAY | MASTER_LOG_POS | RELAY_LOG_POS
    ;

boolMasterOption
    : MASTER_AUTO_POSITION | MASTER_SSL
    | MASTER_SSL_VERIFY_SERVER_CERT
    ;

channelOption
    : FOR CHANNEL STRING_LITERAL
    ;

replicationFilter
    : REPLICATE_DO_DB '=' '(' uidList ')'                           #doDbReplication
    | REPLICATE_IGNORE_DB '=' '(' uidList ')'                       #ignoreDbReplication
    | REPLICATE_DO_TABLE '=' '(' tables ')'                         #doTableReplication
    | REPLICATE_IGNORE_TABLE '=' '(' tables ')'                     #ignoreTableReplication
    | REPLICATE_WILD_DO_TABLE '=' '(' simpleStrings ')'             #wildDoTableReplication
    | REPLICATE_WILD_IGNORE_TABLE
       '=' '(' simpleStrings ')'                                    #wildIgnoreTableReplication
    | REPLICATE_REWRITE_DB '='
      '(' tablePair (',' tablePair)* ')'                            #rewriteDbReplication
    ;

tablePair
    : '(' firstTable=tableName ',' secondTable=tableName ')'
    ;

threadType
    : IO_THREAD | SQL_THREAD
    ;

untilOption
    : gtids=(SQL_BEFORE_GTIDS | SQL_AFTER_GTIDS)
      '=' gtuidSet                                                  #gtidsUntilOption
    | MASTER_LOG_FILE '=' STRING_LITERAL
      ',' MASTER_LOG_POS '=' decimalLiteral                         #masterLogUntilOption
    | RELAY_LOG_FILE '=' STRING_LITERAL
      ',' RELAY_LOG_POS '=' decimalLiteral                          #relayLogUntilOption
    | SQL_AFTER_MTS_GAPS                                            #sqlGapsUntilOption
    ;

connectionOption
    : USER '=' conOptUser=STRING_LITERAL                            #userConnectionOption
    | PASSWORD '=' conOptPassword=STRING_LITERAL                    #passwordConnectionOption
    | DEFAULT_AUTH '=' conOptDefAuth=STRING_LITERAL                 #defaultAuthConnectionOption
    | PLUGIN_DIR '=' conOptPluginDir=STRING_LITERAL                 #pluginDirConnectionOption
    ;

gtuidSet
    : uuidSet (',' uuidSet)*
    | STRING_LITERAL
    ;


//    XA Transactions

xaStartTransaction
    : XA xaStart=(START | BEGIN) xid xaAction=(JOIN | RESUME)?
    ;

xaEndTransaction
    : XA END xid (SUSPEND (FOR MIGRATE)?)?
    ;

xaPrepareStatement
    : XA PREPARE xid
    ;

xaCommitWork
    : XA COMMIT xid (ONE PHASE)?
    ;

xaRollbackWork
    : XA ROLLBACK xid
    ;

xaRecoverWork
    : XA RECOVER (CONVERT xid)?
    ;


// Prepared Statements

prepareStatement
    : PREPARE uid FROM
      (query=STRING_LITERAL | variable=LOCAL_ID)
    ;

executeStatement
    : EXECUTE uid (USING userVariables)?
    ;

deallocatePrepare
    : dropFormat=(DEALLOCATE | DROP) PREPARE uid
    ;


// Compound Statements

routineBody
    : blockStatement | sqlStatement
    ;

// details

blockStatement
    : (uid ':')? BEGIN
      (
        (declareVariable SEMI)*
        (declareCondition SEMI)*
        (declareCursor SEMI)*
        (declareHandler SEMI)*
        procedureSqlStatement+
      )?
      END uid?
    ;

caseStatement
    : CASE (uid | expression)? caseAlternative+
      (ELSE procedureSqlStatement+)?
      END CASE
    ;

ifStatement
    : IF expression
      THEN thenStatements+=procedureSqlStatement+
      elifAlternative*
      (ELSE elseStatements+=procedureSqlStatement+ )?
      END IF
    ;

iterateStatement
    : ITERATE uid
    ;

leaveStatement
    : LEAVE uid
    ;

loopStatement
    : (uid ':')?
      LOOP procedureSqlStatement+
      END LOOP uid?
    ;

repeatStatement
    : (uid ':')?
      REPEAT procedureSqlStatement+
      UNTIL expression
      END REPEAT uid?
    ;

returnStatement
    : RETURN expression
    ;

whileStatement
    : (uid ':')?
      WHILE expression
      DO procedureSqlStatement+
      END WHILE uid?
    ;

cursorStatement
    : CLOSE uid                                                     #CloseCursor
    | FETCH (NEXT? FROM)? uid INTO uidList                          #FetchCursor
    | OPEN uid                                                      #OpenCursor
    ;

// details

declareVariable
    : DECLARE uidList dataType (DEFAULT defaultValue)?
    ;

declareCondition
    : DECLARE uid CONDITION FOR
      ( decimalLiteral | SQLSTATE VALUE? STRING_LITERAL)
    ;

declareCursor
    : DECLARE uid CURSOR FOR selectStatement
    ;

declareHandler
    : DECLARE handlerAction=(CONTINUE | EXIT | UNDO)
      HANDLER FOR
      handlerConditionValue (',' handlerConditionValue)*
      routineBody
    ;

handlerConditionValue
    : decimalLiteral                                                #handlerConditionCode
    | SQLSTATE VALUE? STRING_LITERAL                                #handlerConditionState
    | uid                                                           #handlerConditionName
    | SQLWARNING                                                    #handlerConditionWarning
    | NOT FOUND                                                     #handlerConditionNotfound
    | SQLEXCEPTION                                                  #handlerConditionException
    ;

procedureSqlStatement
    : (compoundStatement | sqlStatement) SEMI
    ;

caseAlternative
    : WHEN (constant | expression)
      THEN procedureSqlStatement+
    ;

elifAlternative
    : ELSEIF expression
      THEN procedureSqlStatement+
    ;

// Administration Statements

//    Account management statements

alterUser
    : ALTER USER
      userSpecification (',' userSpecification)*                    #alterUserMysqlV56
    | ALTER USER ifExists?
        userAuthOption (',' userAuthOption)*
        (
          REQUIRE
          (tlsNone=NONE | tlsOption (AND? tlsOption)* )
        )?
        (WITH userResourceOption+)?
        (userPasswordOption | userLockOption)*                      #alterUserMysqlV57
    ;

createUser
    : CREATE USER userAuthOption (',' userAuthOption)*              #createUserMysqlV56
    | CREATE USER ifNotExists?
        userAuthOption (',' userAuthOption)*
        (
          REQUIRE
          (tlsNone=NONE | tlsOption (AND? tlsOption)* )
        )?
        (WITH userResourceOption+)?
        (userPasswordOption | userLockOption)*                      #createUserMysqlV57
    ;

dropUser
    : DROP USER ifExists? userName (',' userName)*
    ;

grantStatement
    : GRANT privelegeClause (',' privelegeClause)*
      ON
      privilegeObject=(TABLE | FUNCTION | PROCEDURE)?
      privilegeLevel
      TO userAuthOption (',' userAuthOption)*
      (
          REQUIRE
          (tlsNone=NONE | tlsOption (AND? tlsOption)* )
        )?
      (WITH (GRANT OPTION | userResourceOption)* )?
    ;

grantProxy
    : GRANT PROXY ON fromFirst=userName
      TO toFirst=userName (',' toOther+=userName)*
      (WITH GRANT OPTION)?
    ;

renameUser
    : RENAME USER
      renameUserClause (',' renameUserClause)*
    ;

revokeStatement
    : REVOKE privelegeClause (',' privelegeClause)*
      ON
      privilegeObject=(TABLE | FUNCTION | PROCEDURE)?
      privilegeLevel
      FROM userName (',' userName)*                                 #detailRevoke
    | REVOKE ALL PRIVILEGES? ',' GRANT OPTION
      FROM userName (',' userName)*                                 #shortRevoke
    ;

revokeProxy
    : REVOKE PROXY ON onUser=userName
      FROM fromFirst=userName (',' fromOther+=userName)*
    ;

setPasswordStatement
    : SET PASSWORD (FOR userName)?
      '=' ( passwordFunctionClause | STRING_LITERAL)
    ;

// details

userSpecification
    : userName userPasswordOption
    ;

userAuthOption
    : userName IDENTIFIED BY PASSWORD hashed=STRING_LITERAL         #passwordAuthOption
    | userName
      IDENTIFIED (WITH authPlugin)? BY STRING_LITERAL               #stringAuthOption
    | userName
      IDENTIFIED WITH authPlugin
      (AS STRING_LITERAL)?                                          #hashAuthOption
    | userName                                                      #simpleAuthOption
    ;

tlsOption
    : SSL
    | X509
    | CIPHER STRING_LITERAL
    | ISSUER STRING_LITERAL
    | SUBJECT STRING_LITERAL
    ;

userResourceOption
    : MAX_QUERIES_PER_HOUR decimalLiteral
    | MAX_UPDATES_PER_HOUR decimalLiteral
    | MAX_CONNECTIONS_PER_HOUR decimalLiteral
    | MAX_USER_CONNECTIONS decimalLiteral
    ;

userPasswordOption
    : PASSWORD EXPIRE
      (expireType=DEFAULT
      | expireType=NEVER
      | expireType=INTERVAL decimalLiteral DAY
      )?
    ;

userLockOption
    : ACCOUNT lockType=(LOCK | UNLOCK)
    ;

privelegeClause
    : privilege ( '(' uidList ')' )?
    ;

privilege
    : ALL PRIVILEGES?
    | ALTER ROUTINE?
    | CREATE
      (TEMPORARY TABLES | ROUTINE | VIEW | USER | TABLESPACE)?
    | DELETE | DROP | EVENT | EXECUTE | FILE | GRANT OPTION
    | INDEX | INSERT | LOCK TABLES | PROCESS | PROXY
    | REFERENCES | RELOAD
    | REPLICATION (CLIENT | SLAVE)
    | SELECT
    | SHOW (VIEW | DATABASES)
    | SHUTDOWN | SUPER | TRIGGER | UPDATE | USAGE
    ;

privilegeLevel
    : '*'                                                           #currentSchemaPriviLevel
    | '*' '.' '*'                                                   #globalPrivLevel
    | uid '.' '*'                                                   #definiteSchemaPrivLevel
    | uid '.' uid                                                   #definiteFullTablePrivLevel
    | uid                                                           #definiteTablePrivLevel
    ;

renameUserClause
    : fromFirst=userName TO toFirst=userName
    ;

//    Table maintenance statements

analyzeTable
    : ANALYZE actionOption=(NO_WRITE_TO_BINLOG | LOCAL)?
       TABLE tables
    ;

checkTable
    : CHECK TABLE tables checkTableOption*
    ;

checksumTable
    : CHECKSUM TABLE tables actionOption=(QUICK | EXTENDED)?
    ;

optimizeTable
    : OPTIMIZE actionOption=(NO_WRITE_TO_BINLOG | LOCAL)?
      TABLE tables
    ;

repairTable
    : REPAIR actionOption=(NO_WRITE_TO_BINLOG | LOCAL)?
      TABLE tables
      QUICK? EXTENDED? USE_FRM?
    ;

// details

checkTableOption
    : FOR UPGRADE | QUICK | FAST | MEDIUM | EXTENDED | CHANGED
    ;


//    Plugin and udf statements

createUdfunction
    : CREATE AGGREGATE? FUNCTION uid
      RETURNS returnType=(STRING | INTEGER | REAL | DECIMAL)
      SONAME STRING_LITERAL
    ;

installPlugin
    : INSTALL PLUGIN uid SONAME STRING_LITERAL
    ;

uninstallPlugin
    : UNINSTALL PLUGIN uid
    ;


//    Set and show statements

setStatement
    : SET variableClause '=' expression
      (',' variableClause '=' expression)*                          #setVariable
    | SET (CHARACTER SET | CHARSET) (charsetName | DEFAULT)         #setCharset
    | SET NAMES
        (charsetName (COLLATE collationName)? | DEFAULT)            #setNames
    | setPasswordStatement                                          #setPassword
    | setTransactionStatement                                       #setTransaction
    | setAutocommitStatement                                        #setAutocommit
    ;

showStatement
    : SHOW logFormat=(BINARY | MASTER) LOGS                         #showMasterLogs
    | SHOW logFormat=(BINLOG | RELAYLOG)
      EVENTS (IN filename=STRING_LITERAL)?
        (FROM fromPosition=decimalLiteral)?
        (LIMIT
          (offset=decimalLiteral ',')?
          rowCount=decimalLiteral
        )?                                                          #showLogEvents
    | SHOW showCommonEntity showFilter?                             #showObjectFilter
    | SHOW FULL? columnsFormat=(COLUMNS | FIELDS)
      tableFormat=(FROM | IN) tableName
        (schemaFormat=(FROM | IN) uid)? showFilter?                 #showColumns
    | SHOW CREATE schemaFormat=(DATABASE | SCHEMA)
      ifNotExists? uid                                              #showCreateDb
    | SHOW CREATE
        namedEntity=(
          EVENT | FUNCTION | PROCEDURE
          | TABLE | TRIGGER | VIEW
        )
        fullId                                                      #showCreateFullIdObject
    | SHOW CREATE USER userName                                     #showCreateUser
    | SHOW ENGINE engineName engineOption=(STATUS | MUTEX)          #showEngine
    | SHOW showGlobalInfoClause                                     #showGlobalInfo
    | SHOW errorFormat=(ERRORS | WARNINGS)
        (LIMIT
          (offset=decimalLiteral ',')?
          rowCount=decimalLiteral
        )                                                           #showErrors
    | SHOW COUNT '(' '*' ')' errorFormat=(ERRORS | WARNINGS)        #showCountErrors
    | SHOW showSchemaEntity
        (schemaFormat=(FROM | IN) uid)? showFilter?                 #showSchemaFilter
    | SHOW routine=(FUNCTION | PROCEDURE) CODE fullId               #showRoutine
    | SHOW GRANTS (FOR userName)?                                   #showGrants
    | SHOW indexFormat=(INDEX | INDEXES | KEYS)
      tableFormat=(FROM | IN) tableName
        (schemaFormat=(FROM | IN) uid)? (WHERE expression)?         #showIndexes
    | SHOW OPEN TABLES ( schemaFormat=(FROM | IN) uid)?
      showFilter?                                                   #showOpenTables
    | SHOW PROFILE showProfileType (',' showProfileType)*
        (FOR QUERY queryCount=decimalLiteral)?
        (LIMIT
          (offset=decimalLiteral ',')?
          rowCount=decimalLiteral
        )                                                           #showProfile
    | SHOW SLAVE STATUS (FOR CHANNEL STRING_LITERAL)?               #showSlaveStatus
    ;

// details

variableClause
    : LOCAL_ID | GLOBAL_ID | ( ('@' '@')? (GLOBAL | SESSION)  )? uid
    ;

showCommonEntity
    : CHARACTER SET | COLLATION | DATABASES | SCHEMAS
    | FUNCTION STATUS | PROCEDURE STATUS
    | (GLOBAL | SESSION)? (STATUS | VARIABLES)
    ;

showFilter
    : LIKE STRING_LITERAL
    | WHERE expression
    ;

showGlobalInfoClause
    : STORAGE? ENGINES | MASTER STATUS | PLUGINS
    | PRIVILEGES | FULL? PROCESSLIST | PROFILES
    | SLAVE HOSTS | AUTHORS | CONTRIBUTORS
    ;

showSchemaEntity
    : EVENTS | TABLE STATUS | FULL? TABLES | TRIGGERS
    ;

showProfileType
    : ALL | BLOCK IO | CONTEXT SWITCHES | CPU | IPC | MEMORY
    | PAGE FAULTS | SOURCE | SWAPS
    ;


//    Other administrative statements

binlogStatement
    : BINLOG STRING_LITERAL
    ;

cacheIndexStatement
    : CACHE INDEX tableIndexes (',' tableIndexes)*
      ( PARTITION '(' (uidList | ALL) ')' )?
      IN schema=uid
    ;

flushStatement
    : FLUSH flushFormat=(NO_WRITE_TO_BINLOG | LOCAL)?
      flushOption (',' flushOption)*
    ;

killStatement
    : KILL connectionFormat=(CONNECTION | QUERY)?
      decimalLiteral+
    ;

loadIndexIntoCache
    : LOAD INDEX INTO CACHE
      loadedTableIndexes (',' loadedTableIndexes)*
    ;

// remark reset (maser | slave) describe in replication's
//  statements section
resetStatement
    : RESET QUERY CACHE
    ;

shutdownStatement
    : SHUTDOWN
    ;

// details

tableIndexes
    : tableName ( indexFormat=(INDEX | KEY)? '(' uidList ')' )?
    ;

flushOption
    : (
        DES_KEY_FILE | HOSTS
        | (
            BINARY | ENGINE | ERROR | GENERAL | RELAY | SLOW
          )? LOGS
        | OPTIMIZER_COSTS | PRIVILEGES | QUERY CACHE | STATUS
        | USER_RESOURCES | TABLES (WITH READ LOCK)?
       )                                                            #simpleFlushOption
    | RELAY LOGS channelOption?                                     #channelFlushOption
    | TABLES tables flushTableOption?                               #tableFlushOption
    ;

flushTableOption
    : WITH READ LOCK
    | FOR EXPORT
    ;

loadedTableIndexes
    : tableName
      ( PARTITION '(' (partitionList=uidList | ALL) ')' )?
      ( indexFormat=(INDEX | KEY)? '(' indexList=uidList ')' )?
      (IGNORE LEAVES)?
    ;


// Utility Statements


simpleDescribeStatement
    : command=(EXPLAIN | DESCRIBE | DESC) tableName
      (column=uid | pattern=STRING_LITERAL)?
    ;

fullDescribeStatement
    : command=(EXPLAIN | DESCRIBE | DESC)
      (
        formatType=(EXTENDED | PARTITIONS | FORMAT )
        '='
        formatValue=(TRADITIONAL | JSON)
      )?
      describeObjectClause
    ;

helpStatement
    : HELP STRING_LITERAL
    ;

useStatement
    : USE uid
    ;

// details

describeObjectClause
    : (
        selectStatement | deleteStatement | insertStatement
        | replaceStatement | updateStatement
      )                                                             #describeStatements
    | FOR CONNECTION uid                                            #describeConnection
    ;


// Common Clauses

//    DB Objects

fullId
    : uid (DOT_ID | '.' uid)?
    ;

tableName
    : fullId
    ;

fullColumnName
    : uid (dottedId dottedId? )?
    ;

indexColumnName
    : uid ('(' decimalLiteral ')')? sortType=(ASC | DESC)?
    ;

userName
    : STRING_USER_NAME | ID;

mysqlVariable
    : LOCAL_ID
    | GLOBAL_ID
    ;

charsetName
    : BINARY
    | charsetNameBase
    | STRING_LITERAL
    | CHARSET_REVERSE_QOUTE_STRING
    ;

collationName
    : uid | STRING_LITERAL;

engineName
    : ARCHIVE | BLACKHOLE | CSV | FEDERATED | INNODB | MEMORY
    | MRG_MYISAM | MYISAM | NDB | NDBCLUSTER | PERFOMANCE_SCHEMA
    | STRING_LITERAL | REVERSE_QUOTE_ID
    ;

uuidSet
    : decimalLiteral '-' decimalLiteral '-' decimalLiteral
      '-' decimalLiteral '-' decimalLiteral
      (':' decimalLiteral '-' decimalLiteral)+
    ;

xid
    : globalTableUid=xuidStringId
      (
        ',' qualifier=xuidStringId
        (',' idFormat=decimalLiteral)?
      )?
    ;

xuidStringId
    : STRING_LITERAL
    | BIT_STRING
    | HEXADECIMAL_LITERAL+
    ;

authPlugin
    : uid | STRING_LITERAL
    ;

uid
    : simpleId
    //| DOUBLE_QUOTE_ID
    | REVERSE_QUOTE_ID
    | CHARSET_REVERSE_QOUTE_STRING
    ;

simpleId
    : ID
    | charsetNameBase
    | transactionLevelBase
    | engineName
    | privilegesBase
    | intervalTypeBase
    | dataTypeBase
    | keywordsCanBeId
    | functionNameBase
    ;

dottedId
    : DOT_ID
    | '.' uid
    ;


//    Literals

decimalLiteral
    : DECIMAL_LITERAL | ZERO_DECIMAL | ONE_DECIMAL | TWO_DECIMAL
    ;

fileSizeLiteral
    : FILESIZE_LITERAL | decimalLiteral;

stringLiteral
    : (
        STRING_CHARSET_NAME? STRING_LITERAL
        | START_NATIONAL_STRING_LITERAL
      ) STRING_LITERAL+
    | (
        STRING_CHARSET_NAME? STRING_LITERAL
        | START_NATIONAL_STRING_LITERAL
      ) (COLLATE collationName)?
    ;

booleanLiteral
    : TRUE | FALSE;

hexadecimalLiteral
    : STRING_CHARSET_NAME? HEXADECIMAL_LITERAL;

nullNotnull
    : NOT? (NULL_LITERAL | NULL_SPEC_LITERAL)
    ;

constant
    : stringLiteral | decimalLiteral
    | '-' decimalLiteral
    | hexadecimalLiteral | booleanLiteral
    | REAL_LITERAL | BIT_STRING
    | NOT? nullLiteral=(NULL_LITERAL | NULL_SPEC_LITERAL)
    ;


//    Data Types

dataType
    : typeName=(
      CHAR | VARCHAR | TINYTEXT | TEXT | MEDIUMTEXT | LONGTEXT
      )
      lengthOneDimension? BINARY?
      (CHARACTER SET charsetName)? (COLLATE collationName)?         #stringDataType
    | typeName=(
        TINYINT | SMALLINT | MEDIUMINT | INT | INTEGER | BIGINT
      )
      lengthOneDimension? UNSIGNED? ZEROFILL?                       #dimensionDataType
    | typeName=(REAL | DOUBLE | FLOAT)
      lengthTwoDimension? UNSIGNED? ZEROFILL?                       #dimensionDataType
    | typeName=(DECIMAL | NUMERIC)
      lengthTwoOptionalDimension? UNSIGNED? ZEROFILL?               #dimensionDataType
    | typeName=(
        DATE | TINYBLOB | BLOB | MEDIUMBLOB | LONGBLOB
        | BOOL | BOOLEAN
      )                                                             #simpleDataType
    | typeName=(
        BIT | TIME | TIMESTAMP | DATETIME | BINARY
        | VARBINARY | YEAR
      )
      lengthOneDimension?                                           #dimensionDataType
    | typeName=(ENUM | SET)
      '(' STRING_LITERAL (',' STRING_LITERAL)* ')' BINARY?
      (CHARACTER SET charsetName)? (COLLATE collationName)?         #collectionDataType
    | typeName=(
        GEOMETRYCOLLECTION | LINESTRING | MULTILINESTRING
        | MULTIPOINT | MULTIPOLYGON | POINT | POLYGON
      )                                                             #spatialDataType
    ;

convertedDataType
    : typeName=(BINARY| NCHAR) lengthOneDimension?
    | typeName=CHAR lengthOneDimension? (CHARACTER SET charsetName)?
    | typeName=(DATE | DATETIME | TIME)
    | typeName=DECIMAL lengthTwoDimension?
    | (SIGNED | UNSIGNED) INTEGER?
    ;

lengthOneDimension
    : '(' decimalLiteral ')'
    ;

lengthTwoDimension
    : '(' decimalLiteral ',' decimalLiteral ')'
    ;

lengthTwoOptionalDimension
    : '(' decimalLiteral (',' decimalLiteral)? ')'
    ;


//    Common Lists

uidList
    : uid (',' uid)*
    ;

tables
    : tableName (',' tableName)*
    ;

indexColumnNames
    : '(' indexColumnName (',' indexColumnName)* ')'
    ;

expressions
    : expression (',' expression)*
    ;

expressionsWithDefaults
    : expressionOrDefault (',' expressionOrDefault)*
    ;

constants
    : constant (',' constant)*
    ;

simpleStrings
    : STRING_LITERAL (',' STRING_LITERAL)*
    ;

userVariables
    : LOCAL_ID (',' LOCAL_ID)*
    ;


//    Common Expressons

defaultValue
    : NULL_LITERAL
    | constant
    | currentTimestamp (ON UPDATE currentTimestamp)?
    ;

currentTimestamp
    :
    (
      (CURRENT_TIMESTAMP | LOCALTIME | LOCALTIMESTAMP) ('(' decimalLiteral? ')')?
      | NOW '(' decimalLiteral? ')'
    )
    ;

expressionOrDefault
    : expression | DEFAULT
    ;

ifExists
    : IF EXISTS;

ifNotExists
    : IF NOT EXISTS;


//    Functions

functionCall
    : specificFunction                                              #specificFunctionCall
    | aggregateWindowedFunction                                     #aggregateFunctionCall
    | scalarFunctionName '(' functionArgs? ')'                      #scalarFunctionCall
    | fullId '(' functionArgs? ')'                                  #udfFunctionCall
    | passwordFunctionClause                                        #passwordFunctionCall
    ;

specificFunction
    : (
      CURRENT_DATE | CURRENT_TIME | CURRENT_TIMESTAMP
      | CURRENT_USER | LOCALTIME
      )                                                             #simpleFunctionCall
    | CONVERT '(' expression separator=',' convertedDataType ')'    #dataTypeFunctionCall
    | CONVERT '(' expression USING charsetName ')'                  #dataTypeFunctionCall
    | CAST '(' expression AS convertedDataType ')'                  #dataTypeFunctionCall
    | VALUES '(' fullColumnName ')'                                 #valuesFunctionCall
    | CASE expression caseFuncAlternative+
      (ELSE elseArg=functionArg)? END                               #caseFunctionCall
    | CASE caseFuncAlternative+
      (ELSE elseArg=functionArg)? END                               #caseFunctionCall
    | CHAR '(' functionArgs  (USING charsetName)? ')'               #charFunctionCall
    | POSITION
      '('
          (
            positionString=stringLiteral
            | positionExpression=expression
          )
          IN
          (
            inString=stringLiteral
            | inExpression=expression
          )
      ')'                                                           #positionFunctionCall
    | (SUBSTR | SUBSTRING)
      '('
        (
          sourceString=stringLiteral
          | sourceExpression=expression
        ) FROM
        (
          fromDecimal=decimalLiteral
          | fromExpression=expression
        )
        (
          FOR
          (
            forDecimal=decimalLiteral
            | forExpression=expression
          )
        )?
      ')'                                                           #substrFunctionCall
    | TRIM
      '('
        positioinForm=(BOTH | LEADING | TRAILING)
        (
          sourceString=stringLiteral
          | sourceExpression=expression
        )?
        FROM
        (
          fromString=stringLiteral
          | fromExpression=expression
        )
      ')'                                                           #trimFunctionCall
    | TRIM
      '('
        (
          sourceString=stringLiteral
          | sourceExpression=expression
        )
        FROM
        (
          fromString=stringLiteral
          | fromExpression=expression
        )
      ')'                                                           #trimFunctionCall
    | WEIGHT_STRING
      '('
        (stringLiteral | expression)
        (AS stringFormat=(CHAR | BINARY)
        '(' decimalLiteral ')' )?  levelsInWeightString?
      ')'                                                           #weightFunctionCall
    | EXTRACT
      '('
        intervalType
        FROM
        (
          sourceString=stringLiteral
          | sourceExpression=expression
        )
      ')'                                                           #extractFunctionCall
    | GET_FORMAT
      '('
        datetimeFormat=(DATE | TIME | DATETIME)
        ',' stringLiteral
      ')'                                                           #getFormatFunctionCall
    ;

caseFuncAlternative
    : WHEN condition=functionArg
      THEN consequent=functionArg
    ;

levelsInWeightString
    : LEVEL levelInWeightListElement
      (',' levelInWeightListElement)*                               #levelWeightList
    | LEVEL
      firstLevel=decimalLiteral '-' lastLevel=decimalLiteral        #levelWeightRange
    ;

levelInWeightListElement
    : decimalLiteral orderType=(ASC | DESC | REVERSE)?
    ;

aggregateWindowedFunction
    : (AVG | MAX | MIN | SUM)
      '(' aggregator=(ALL | DISTINCT)? functionArg ')'
    | COUNT '(' (starArg='*' | aggregator=ALL? functionArg) ')'
    | COUNT '(' aggregator=DISTINCT functionArgs ')'
    | (
        BIT_AND | BIT_OR | BIT_XOR | STD | STDDEV | STDDEV_POP
        | STDDEV_SAMP | VAR_POP | VAR_SAMP | VARIANCE
      ) '(' aggregator=ALL? functionArg ')'
    | GROUP_CONCAT '('
        aggregator=DISTINCT? functionArgs
        (ORDER BY
          orderByExpression (',' orderByExpression)*
        )? (SEPARATOR separator=STRING_LITERAL)?
      ')'
    ;

scalarFunctionName
    : functionNameBase
    | ASCII | CURDATE | CURRENT_DATE | CURRENT_TIME
    | CURRENT_TIMESTAMP | CURTIME | DATE_ADD | DATE_SUB
    | IF | INSERT | LOCALTIME | LOCALTIMESTAMP | MID | NOW
    | REPLACE | SUBSTR | SUBSTRING | SYSDATE | TRIM
    | UTC_DATE | UTC_TIME | UTC_TIMESTAMP
    ;

passwordFunctionClause
    : functionName=(PASSWORD | OLD_PASSWORD) '(' functionArg ')'
    ;

functionArgs
    : (constant | fullColumnName | functionCall | expression)
    (
      ','
      (constant | fullColumnName | functionCall | expression)
    )*
    ;

functionArg
    : constant | fullColumnName | functionCall | expression
    ;


//    Expressions, predicates

// Simplified approach for expression
expression
    : notOperator=(NOT | '!') expression                            #notExpression
    | expression logicalOperator expression                         #logicalExpression
    | predicate IS NOT? testValue=(TRUE | FALSE | UNKNOWN)          #isExpression
    | predicate                                                     #predicateExpression
    ;

predicate
    : predicate NOT? IN '(' (selectStatement | expressions) ')'     #inPredicate
    | predicate IS nullNotnull                                      #isNullPredicate
    | left=predicate comparisonOperator right=predicate             #binaryComparasionPredicate
    | predicate comparisonOperator
      quantifier=(ALL | ANY | SOME) '(' selectStatement ')'         #subqueryComparasionPredicate
    | predicate NOT? BETWEEN predicate AND predicate                #betweenPredicate
    | predicate SOUNDS LIKE predicate                               #soundsLikePredicate
    | predicate NOT? LIKE predicate (ESCAPE STRING_LITERAL)?        #likePredicate
    | predicate NOT? regex=(REGEXP | RLIKE) predicate               #regexpPredicate
    | (LOCAL_ID VAR_ASSIGN)? expressionAtom                         #expressionAtomPredicate
    ;


// Add in ASTVisitor nullNotnull in constant
expressionAtom
    : constant                                                      #constantExpressionAtom
    | fullColumnName                                                #fullColumnNameExpressionAtom
    | functionCall                                                  #functionCallExpressionAtom
    | expressionAtom COLLATE collationName                          #collateExpressionAtom
    | mysqlVariable                                                 #mysqlVariableExpressionAtom
    | unaryOperator expressionAtom                                  #unaryExpressionAtom
    | BINARY expressionAtom                                         #binaryExpressionAtom
    | '(' expression (',' expression)* ')'                          #nestedExpressionAtom
    | ROW '(' expression (',' expression)+ ')'                      #nestedRowExpressionAtom
    | EXISTS '(' selectStatement ')'                                #existsExpessionAtom
    | '(' selectStatement ')'                                       #subqueryExpessionAtom
    | INTERVAL expression intervalType                              #intervalExpressionAtom
    | left=expressionAtom bitOperator right=expressionAtom          #bitExpressionAtom
    | left=expressionAtom mathOperator right=expressionAtom         #mathExpressionAtom
    ;

unaryOperator
    : '!' | '~' | '+' | '-' | NOT
    ;

comparisonOperator
    : '=' | '>' | '<' | '<' '=' | '>' '='
    | '<' '>' | '!' '=' | '<' '=' '>'
    ;

logicalOperator
    : AND | '&' '&' | XOR | OR | '|' '|'
    ;

bitOperator
    : '<' '<' | '>' '>' | '&' | '^' | '|'
    ;

mathOperator
    : '*' | '/' | '%' | DIV | MOD | '+' | '-' | '--'
    ;


//    Simple id sets
//     (that keyword, which can be id)

charsetNameBase
    : ARMSCII8 | ASCII | BIG5 | CP1250 | CP1251 | CP1256 | CP1257
    | CP850 | CP852 | CP866 | CP932 | DEC8 | EUCJPMS | EUCKR
    | GB2312 | GBK | GEOSTD8 | GREEK | HEBREW | HP8 | KEYBCS2
    | KOI8R | KOI8U | LATIN1 | LATIN2 | LATIN5 | LATIN7 | MACCE
    | MACROMAN | SJIS | SWE7 | TIS620 | UCS2 | UJIS | UTF16
    | UTF16LE | UTF32 | UTF8 | UTF8MB3 | UTF8MB4
    ;

transactionLevelBase
    : REPEATABLE | COMMITTED | UNCOMMITTED | SERIALIZABLE
    ;

privilegesBase
    : TABLES | ROUTINE | EXECUTE | FILE | PROCESS
    | RELOAD | SHUTDOWN | SUPER | PRIVILEGES
    ;

intervalTypeBase
    : QUARTER | MONTH | DAY | HOUR
    | MINUTE | WEEK | SECOND | MICROSECOND
    ;

dataTypeBase
    : DATE | TIME | TIMESTAMP | DATETIME | YEAR | ENUM | TEXT
    ;

keywordsCanBeId
    : ACCOUNT | ACTION | AFTER | AGGREGATE | ALGORITHM | ANY
    | AT | AUTHORS | AUTOCOMMIT | AUTOEXTEND_SIZE
    | AUTO_INCREMENT | AVG_ROW_LENGTH | BEGIN | BINLOG | BIT
    | BLOCK | BOOL | BOOLEAN | BTREE | CASCADED | CHAIN
    | CHANNEL | CHECKSUM | CIPHER | CLIENT | COALESCE | CODE
    | COLUMNS | COLUMN_FORMAT | COMMENT | COMMIT | COMPACT
    | COMPLETION | COMPRESSED | COMPRESSION | CONCURRENT
    | CONNECTION | CONSISTENT | CONTAINS | CONTEXT
    | CONTRIBUTORS | COPY | CPU | DATA | DATAFILE | DEALLOCATE
    | DEFAULT_AUTH | DEFINER | DELAY_KEY_WRITE | DIRECTORY
    | DISABLE | DISCARD | DISK | DO | DUMPFILE | DUPLICATE
    | DYNAMIC | ENABLE | ENCRYPTION | ENDS | ENGINE | ENGINES
    | ERROR | ERRORS | ESCAPE | EVEN | EVENT | EVENTS | EVERY
    | EXCHANGE | EXCLUSIVE | EXPIRE | EXTENT_SIZE | FAULTS
    | FIELDS | FILE_BLOCK_SIZE | FILTER | FIRST | FIXED
    | FOLLOWS | FULL | FUNCTION | GLOBAL | GRANTS
    | GROUP_REPLICATION | HASH | HOST | IDENTIFIED
    | IGNORE_SERVER_IDS | IMPORT | INDEXES | INITIAL_SIZE
    | INPLACE | INSERT_METHOD | INSTANCE | INVOKER | IO
    | IO_THREAD | IPC | ISOLATION | ISSUER | KEY_BLOCK_SIZE
    | LANGUAGE | LAST | LEAVES | LESS | LEVEL | LIST | LOCAL
    | LOGFILE | LOGS | MASTER | MASTER_AUTO_POSITION
    | MASTER_CONNECT_RETRY | MASTER_DELAY
    | MASTER_HEARTBEAT_PERIOD | MASTER_HOST | MASTER_LOG_FILE
    | MASTER_LOG_POS | MASTER_PASSWORD | MASTER_PORT
    | MASTER_RETRY_COUNT | MASTER_SSL | MASTER_SSL_CA
    | MASTER_SSL_CAPATH | MASTER_SSL_CERT | MASTER_SSL_CIPHER
    | MASTER_SSL_CRL | MASTER_SSL_CRLPATH | MASTER_SSL_KEY
    | MASTER_TLS_VERSION | MASTER_USER
    | MAX_CONNECTIONS_PER_HOUR | MAX_QUERIES_PER_HOUR
    | MAX_ROWS | MAX_SIZE | MAX_UPDATES_PER_HOUR
    | MAX_USER_CONNECTIONS | MEMORY | MERGE | MID | MIGRATE
    | MIN_ROWS | MODIFY | MUTEX | MYSQL | NAME | NAMES
    | NCHAR | NEVER | NO | NODEGROUP | NONE | OFFLINE | OFFSET
    | OJ | OLD_PASSWORD | ONE | ONLINE | ONLY | OPTIMIZER_COSTS
    | OPTIONS | OWNER | PACK_KEYS | PAGE | PARSER | PARTIAL
    | PARTITIONING | PARTITIONS | PASSWORD | PHASE | PLUGINS
    | PLUGIN_DIR | PORT | PRECEDES | PREPARE | PRESERVE | PREV
    | PROCESSLIST | PROFILE | PROFILES | PROXY | QUERY | QUICK
    | REBUILD | RECOVER | REDO_BUFFER_SIZE | REDUNDANT
    | RELAYLOG | RELAY_LOG_FILE | RELAY_LOG_POS | REMOVE
    | REORGANIZE | REPAIR | REPLICATE_DO_DB | REPLICATE_DO_TABLE
    | REPLICATE_IGNORE_DB | REPLICATE_IGNORE_TABLE
    | REPLICATE_REWRITE_DB | REPLICATE_WILD_DO_TABLE
    | REPLICATE_WILD_IGNORE_TABLE | REPLICATION | RESUME
    | RETURNS | ROLLBACK | ROLLUP | ROTATE | ROW | ROWS
    | ROW_FORMAT | SAVEPOINT | SCHEDULE | SECURITY | SERVER
    | SESSION | SHARE | SHARED | SIGNED | SIMPLE | SLAVE
    | SNAPSHOT | SOCKET | SOME | SOUNDS | SOURCE
    | SQL_AFTER_GTIDS | SQL_AFTER_MTS_GAPS | SQL_BEFORE_GTIDS
    | SQL_BUFFER_RESULT | SQL_CACHE | SQL_NO_CACHE | SQL_THREAD
    | START | STARTS | STATS_AUTO_RECALC | STATS_PERSISTENT
    | STATS_SAMPLE_PAGES | STATUS | STOP | STORAGE | STRING
    | SUBJECT | SUBPARTITION | SUBPARTITIONS | SUSPEND | SWAPS
    | SWITCHES | TABLESPACE | TEMPORARY | TEMPTABLE | THAN
    | TRANSACTION | TRUNCATE | UNDEFINED | UNDOFILE
    | UNDO_BUFFER_SIZE | UNKNOWN | UPGRADE | USER | VALIDATION
    | VALUE | VARIABLES | VIEW | WAIT | WARNINGS | WITHOUT
    | WORK | WRAPPER | X509 | XA | XML
    ;

functionNameBase
    : ABS | ACOS | ADDDATE | ADDTIME | AES_DECRYPT | AES_ENCRYPT
    | AREA | ASBINARY | ASIN | ASTEXT | ASWKB | ASWKT
    | ASYMMETRIC_DECRYPT | ASYMMETRIC_DERIVE
    | ASYMMETRIC_ENCRYPT | ASYMMETRIC_SIGN | ASYMMETRIC_VERIFY
    | ATAN | ATAN2 | BENCHMARK | BIN | BIT_COUNT | BIT_LENGTH
    | BUFFER | CEIL | CEILING | CENTROID | CHARACTER_LENGTH
    | CHARSET | CHAR_LENGTH | COERCIBILITY | COLLATION
    | COMPRESS | CONCAT | CONCAT_WS | CONNECTION_ID | CONV
    | CONVERT_TZ | COS | COT | COUNT | CRC32
    | CREATE_ASYMMETRIC_PRIV_KEY | CREATE_ASYMMETRIC_PUB_KEY
    | CREATE_DH_PARAMETERS | CREATE_DIGEST | CROSSES | DATABASE | DATE
    | DATEDIFF | DATE_FORMAT | DAY | DAYNAME | DAYOFMONTH
    | DAYOFWEEK | DAYOFYEAR | DECODE | DEGREES | DES_DECRYPT
    | DES_ENCRYPT | DIMENSION | DISJOINT | ELT | ENCODE
    | ENCRYPT | ENDPOINT | ENVELOPE | EQUALS | EXP | EXPORT_SET
    | EXTERIORRING | EXTRACTVALUE | FIELD | FIND_IN_SET | FLOOR
    | FORMAT | FOUND_ROWS | FROM_BASE64 | FROM_DAYS
    | FROM_UNIXTIME | GEOMCOLLFROMTEXT | GEOMCOLLFROMWKB
    | GEOMETRYCOLLECTION | GEOMETRYCOLLECTIONFROMTEXT
    | GEOMETRYCOLLECTIONFROMWKB | GEOMETRYFROMTEXT
    | GEOMETRYFROMWKB | GEOMETRYN | GEOMETRYTYPE | GEOMFROMTEXT
    | GEOMFROMWKB | GET_FORMAT | GET_LOCK | GLENGTH | GREATEST
    | GTID_SUBSET | GTID_SUBTRACT | HEX | HOUR | IFNULL
    | INET6_ATON | INET6_NTOA | INET_ATON | INET_NTOA | INSTR
    | INTERIORRINGN | INTERSECTS | ISCLOSED | ISEMPTY | ISNULL
    | ISSIMPLE | IS_FREE_LOCK | IS_IPV4 | IS_IPV4_COMPAT
    | IS_IPV4_MAPPED | IS_IPV6 | IS_USED_LOCK | LAST_INSERT_ID
    | LCASE | LEAST | LEFT | LENGTH | LINEFROMTEXT | LINEFROMWKB
    | LINESTRING | LINESTRINGFROMTEXT | LINESTRINGFROMWKB | LN
    | LOAD_FILE | LOCATE | LOG | LOG10 | LOG2 | LOWER | LPAD
    | LTRIM | MAKEDATE | MAKETIME | MAKE_SET | MASTER_POS_WAIT
    | MBRCONTAINS | MBRDISJOINT | MBREQUAL | MBRINTERSECTS
    | MBROVERLAPS | MBRTOUCHES | MBRWITHIN | MD5 | MICROSECOND
    | MINUTE | MLINEFROMTEXT | MLINEFROMWKB | MONTH | MONTHNAME
    | MPOINTFROMTEXT | MPOINTFROMWKB | MPOLYFROMTEXT
    | MPOLYFROMWKB | MULTILINESTRING | MULTILINESTRINGFROMTEXT
    | MULTILINESTRINGFROMWKB | MULTIPOINT | MULTIPOINTFROMTEXT
    | MULTIPOINTFROMWKB | MULTIPOLYGON | MULTIPOLYGONFROMTEXT
    | MULTIPOLYGONFROMWKB | NAME_CONST | NULLIF | NUMGEOMETRIES
    | NUMINTERIORRINGS | NUMPOINTS | OCT | OCTET_LENGTH | ORD
    | OVERLAPS | PERIOD_ADD | PERIOD_DIFF | PI | POINT
    | POINTFROMTEXT | POINTFROMWKB | POINTN | POLYFROMTEXT
    | POLYFROMWKB | POLYGON | POLYGONFROMTEXT | POLYGONFROMWKB
    | POSITION| POW | POWER | QUARTER | QUOTE | RADIANS | RAND
    | RANDOM_BYTES | RELEASE_LOCK | REVERSE | RIGHT | ROUND
    | ROW_COUNT | RPAD | RTRIM | SECOND | SEC_TO_TIME
    | SESSION_USER | SHA | SHA1 | SHA2 | SIGN | SIN | SLEEP
    | SOUNDEX | SQL_THREAD_WAIT_AFTER_GTIDS | SQRT | SRID
    | STARTPOINT | STRCMP | STR_TO_DATE | ST_AREA | ST_ASBINARY
    | ST_ASTEXT | ST_ASWKB | ST_ASWKT | ST_BUFFER | ST_CENTROID
    | ST_CONTAINS | ST_CROSSES | ST_DIFFERENCE | ST_DIMENSION
    | ST_DISJOINT | ST_DISTANCE | ST_ENDPOINT | ST_ENVELOPE
    | ST_EQUALS | ST_EXTERIORRING | ST_GEOMCOLLFROMTEXT
    | ST_GEOMCOLLFROMTXT | ST_GEOMCOLLFROMWKB
    | ST_GEOMETRYCOLLECTIONFROMTEXT
    | ST_GEOMETRYCOLLECTIONFROMWKB | ST_GEOMETRYFROMTEXT
    | ST_GEOMETRYFROMWKB | ST_GEOMETRYN | ST_GEOMETRYTYPE
    | ST_GEOMFROMTEXT | ST_GEOMFROMWKB | ST_INTERIORRINGN
    | ST_INTERSECTION | ST_INTERSECTS | ST_ISCLOSED | ST_ISEMPTY
    | ST_ISSIMPLE | ST_LINEFROMTEXT | ST_LINEFROMWKB
    | ST_LINESTRINGFROMTEXT | ST_LINESTRINGFROMWKB
    | ST_NUMGEOMETRIES | ST_NUMINTERIORRING
    | ST_NUMINTERIORRINGS | ST_NUMPOINTS | ST_OVERLAPS
    | ST_POINTFROMTEXT | ST_POINTFROMWKB | ST_POINTN
    | ST_POLYFROMTEXT | ST_POLYFROMWKB | ST_POLYGONFROMTEXT
    | ST_POLYGONFROMWKB | ST_SRID | ST_STARTPOINT
    | ST_SYMDIFFERENCE | ST_TOUCHES | ST_UNION | ST_WITHIN
    | ST_X | ST_Y | SUBDATE | SUBSTRING_INDEX | SUBTIME
    | SYSTEM_USER | TAN | TIME | TIMEDIFF | TIMESTAMP
    | TIMESTAMPADD | TIMESTAMPDIFF | TIME_FORMAT | TIME_TO_SEC
    | TOUCHES | TO_BASE64 | TO_DAYS | TO_SECONDS | UCASE
    | UNCOMPRESS | UNCOMPRESSED_LENGTH | UNHEX | UNIX_TIMESTAMP
    | UPDATEXML | UPPER | UUID | UUID_SHORT
    | VALIDATE_PASSWORD_STRENGTH | VERSION
    | WAIT_UNTIL_SQL_THREAD_AFTER_GTIDS | WEEK | WEEKDAY
    | WEEKOFYEAR | WEIGHT_STRING | WITHIN | YEAR | YEARWEEK
    | Y_FUNCTION | X_FUNCTION
    ;