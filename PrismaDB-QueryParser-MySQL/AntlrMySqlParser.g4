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
    : insertFormat=VALUES
      '(' expressions ')'
        (',' '(' expressions ')')*
    ;

updatedElement
    : fullColumnName '=' (expression | DEFAULT)
    ;

assignmentField
    : uid
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
    : tableSourceItem (',' tableSourceItem)*
    ;

tableSourceItem
    : tableName
      (AS? alias=uid)?
    ;

joinPart
    : (INNER | CROSS)? JOIN tableSourceItem
      (
        ON expression
      )?                                                            #innerJoin
    | (LEFT | RIGHT) OUTER? JOIN tableSourceItem
        (
          ON expression
        )                                                           #outerJoin
    ;

//    Select Statement's Details

queryExpression
    : '(' querySpecification ')'
    | '(' queryExpression ')'
    ;

querySpecification
    : SELECT selectElements
      fromClause? orderByClause? limitClause?
    ;

// details

selectElements
    : (star='*' | selectElement ) (',' selectElement)*
    ;

selectElement
    : uid '.' '*'                                                   #selectStarElement
    | fullColumnName (AS? uid)?                                     #selectColumnElement
    | functionCall (AS? uid)?                                       #selectFunctionElement
    | expression (AS? uid)?                                         #selectExpressionElement
    ;

fromClause
    : FROM tableSources joinPart*
      (WHERE whereExpr=expression)?
      (
        GROUP BY
        groupByItem (',' groupByItem)*
      )?
    ;

groupByItem
    : expression
    ;

limitClause
    : LIMIT
    (
      limit=intLiteral
    )
    ;


// Utility Statements


useStatement
    : USE uid
    ;


// Common Clauses

//    DB Objects

tableName
    : uid
    ;

fullColumnName
    : uid dottedId?
    ;

mysqlVariable
    : GLOBAL_ID
    ;

uid
    : simpleId
    | REVERSE_QUOTE_ID
    ;

simpleId
    : ID
    ;

dottedId
    : DOT_ID
    | '.' uid
    ;


//    Literals

intLiteral
    : INT_LITERAL
    ;

decimalLiteral
    : DECIMAL_LITERAL
    ;

stringLiteral
    : STRING_LITERAL
    ;

hexadecimalLiteral
    : HEXADECIMAL_LITERAL
    ;

nullNotnull
    : NOT? (NULL_LITERAL | NULL_SPEC_LITERAL)
    ;

constant
    : intLiteral | stringLiteral
    | decimalLiteral | hexadecimalLiteral
    | nullLiteral=(NULL_LITERAL | NULL_SPEC_LITERAL)
    ;


//    Data Types

dataType
    : typeName=(
      CHAR | VARCHAR | TEXT
      )
      lengthOneDimension?                                           #stringDataType
    | typeName=(
        TINYINT | SMALLINT | MEDIUMINT | INT | BIGINT | DOUBLE |
        DATE | TIMESTAMP | DATETIME | BLOB
      )                                                             #simpleDataType
    | typeName=(
        BINARY | VARBINARY
      )
      lengthOneDimension?                                           #dimensionDataType
    |  typeName=ENUM
      '(' STRING_LITERAL (',' STRING_LITERAL)* ')'                  #collectionDataType
    ;

lengthOneDimension
    : '(' intLiteral ')'
    ;

lengthTwoDimension
    : '(' intLiteral ',' intLiteral ')'
    ;

lengthTwoOptionalDimension
    : '(' intLiteral (',' intLiteral)? ')'
    ;


//    Common Lists

uidList
    : uid (',' uid)*
    ;

tables
    : tableName (',' tableName)*
    ;

expressions
    : expression (',' expression)*
    ;

constants
    : constant (',' constant)*
    ;

simpleStrings
    : STRING_LITERAL (',' STRING_LITERAL)*
    ;


//    Common Expressons

defaultValue
    : constant
    | currentTimestamp
    ;

currentTimestamp
    :
    (
      (CURRENT_TIMESTAMP)
    )
    ;


//    Functions

functionCall
    : specificFunction                                              #specificFunctionCall
    | scalarFunctionName '(' functionArgs? ')'                      #scalarFunctionCall
    | uid '(' functionArgs? ')'                                     #udfFunctionCall
    ;

specificFunction
    : (
      CURRENT_TIMESTAMP
      )                                                             #simpleFunctionCall
    ;

scalarFunctionName
    : SUM | AVG | ABS | COUNT | MIN | MAX
    | NOW | DATE | UTC_TIMESTAMP | TIMEDIFF
    ;

functionArgs
    : functionArg
    (
      ','
      functionArg
    )*
    ;

functionArg
    : constant | fullColumnName | functionCall | expression | star='*'
    ;


//    Expressions, predicates

// Simplified approach for expression
expression
    : notOperator=(NOT | '!') expression                            #notExpression
    | expression logicalOperator expression                         #logicalExpression
    | predicate                                                     #predicateExpression
	| '(' (expression) ')'                                          #nestedExpression
    ;

predicate
    : predicate NOT? IN '(' (expressions) ')'                       #inPredicate
    | predicate IS nullNotnull                                      #isNullPredicate
    | left=predicate comparisonOperator right=predicate             #binaryComparasionPredicate
    | predicate NOT? LIKE predicate                                 #likePredicate
    | expressionAtom                                                #expressionAtomPredicate
	| '(' (predicate) ')'                                           #nestedPredicate
    ;


// Add in ASTVisitor nullNotnull in constant
expressionAtom
    : constant                                                      #constantExpressionAtom
    | fullColumnName                                                #fullColumnNameExpressionAtom
    | functionCall                                                  #functionCallExpressionAtom
    | mysqlVariable                                                 #mysqlVariableExpressionAtom
    | unaryOperator expressionAtom                                  #unaryExpressionAtom
    | left=expressionAtom mathOperator right=expressionAtom         #mathExpressionAtom
	| '(' (expressionAtom) ')'                                      #nestedExpressionAtom
    ;

unaryOperator
    : '!' | NOT
    ;

comparisonOperator
    : '=' | '>' | '<' | '<' '=' | '>' '='
    | '<' '>' | '!' '='
    ;

logicalOperator
    : AND | OR
    ;

mathOperator
    : '*' | '/' | '+' | '-'
    ;