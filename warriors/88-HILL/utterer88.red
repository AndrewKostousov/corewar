;redcode
;name Utterer '88
;author Christian Schmidt
;strategy qscanner, spreader
;assert CORESIZE==8000

sOff   equ 2628
sAwa   equ 6490

iOff   equ 7542
iAwa   equ 3551

boot	  spl	1
	  spl	1
        spl     iBoo

sBoo    mov     <sSrc,  <sDist
sSrc    spl     @sDist, sEnd+1
        add     #sAwa,  sDist
        add     #1,     sSrc
sDist   jmp     sBoo,   sOff

iBoo    mov     <iSrc,  <iDist
iSrc    spl     @iDist, imp+1
        add     #iAwa,  iDist
        add     #1,     iSrc
iDist   jmp     iBoo,   iOff

step   equ    82
time   equ  2437
dd     equ     7

stone  mov <dd+1+(2*time), 3+(step*time)
       spl  -1, <2-step
       add   1,   -2
sEnd   djn  -2, <0-step

vortex spl	0
	 add	#2667	, launch
launch jmp	@0	, imp-2668
imp	 mov	0	, 2667


   for 40
       dat   #0,       #0
   rof

qs    equ 322
qd    equ 161

qscan cmp 2*qs+qd       , 2*qs
qt1   jmp qa0           , <3*qs
      cmp qscan+5*qs+qd , qscan+5*qs
qt2   jmp qa1           , <4*qs
      cmp qscan+4*qs+qd , qscan+4*qs
qs1   djn qa1           , #qt1
      cmp qscan+10*qs-2 , qscan+10*qs+qd-2
qs2   djn qa2           , #qt2
      cmp qscan+9*qs+qd , qscan+9*qs
qt3   jmp qa2           , <6*qs
      cmp qscan+6*qs+qd , qscan+6*qs
      jmp qa2           , <qa1
      cmp qscan+8*qs+qd , qscan+8*qs
      jmp qa2           , <qs1
      cmp qscan+11*qs   , qscan+11*qs+qd
      jmp qa3           , <qa2
      cmp qscan+18*qs-8 , qscan+18*qs+qd-8
qs3   djn qa3           , #qt3
      cmp qscan+16*qs-2 , qscan+16*qs+qd-2
      jmp qa3           , <qs2
      cmp qscan+12*qs   , qscan+12*qs+qd
      jmp qa3           , <qa1
      cmp qscan+14*qs   , qscan+14*qs+qd
      jmp qa3           , <qs1
      jmz boot          , qscan+15*qs

qa3   add @qs3          , qp
qa2   add @qs2          , @qa3
qa1   add @qs1          , @qa3
qa0   cmp @qp           , <1234
      cmp @0            , 0
      add #qd           , qp
ql    mov qb            , @qp
qp    mov <2345         , <qscan+2*qs
      sub #9            , @ql
      djn ql            , #6
qb    jmp boot          , <43

      end qscan

