;REDCODE
;name G007W059
;author RC94 Evolover
;Parents: G006W041 G006W035
org 2
DAT.I  @1027,>0041
JMZ.F  >0041,>0049
SPL.A  #0041,<0007
JMP.X  >2184,@0016
SPL.A  #0033,<0007
DIV.F  #4133,@0029
SPL.A  #0033,<0007
JMZ.F  >0105,>0049
MUL.A  <0042,<0006
MUL.A  $0042,<1030
SPL.A  #0041,<0007
SPL.A  #0033,<0007
MOV.A  #0037,<0135
JMN.A  <0016,>0042
SPL.F  #4099,$0048
JMZ.F  >0041,>0023
SPL.F  #0019,$0048
JMZ.BA  @1073,$0037
SUB.X  $0037,@1063
DIV.X  @0020,$0047
SPL.BA  #0009,$0043
JMZ.AB  @1032,#0045
JMZ.AB  @1032,#0045
MOV.F  #0005,@1053
CMP.X  >0008,@0016
ADD.AB  @0146,$0005
ADD.BA  >0031,@0054
MOV.B  @0010,>0616
SUB.AB  <0003,#0043
MUL.B  <0003,@0043
DAT.I  >0032,>0045
DAT.I  $0042,$4147
SPL.F  >0147,$0032
MOD.BA  >0007,#0051
JMP.X  >0010,@0016
ADD.AB  @0026,$0029
MOV.A  #0037,<0007
JMN.F  #0017,>0042
SUB.X  @0004,$0047
JMN.A  #0144,>0042
SUB.X  @0004,<0047
DAT.I  $0042,$0019
ADD.AB  @0026,$1037
DAT.I  >1054,$0015
CMP.B  #0007,>0016
MUL.A  $0010,<0070
JMZ.A  @0260,$0007
SPL.F  @0019,$1074
SLT.X  #0022,@2052
JMZ.BA  @0049,$0037
SPL.F  #0018,$0048
CMP.BA  <0030,$0004
DAT.I  >1054,$0014
DIV.F  #0526,#0046
SLT.X  #0022,@2072
SPL.F  #0526,#0046
SLT.A  @0002,>0104
DAT.I  >0032,>0045
DAT.I  $0042,$4147
