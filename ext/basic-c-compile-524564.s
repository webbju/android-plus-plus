	.syntax unified
	.cpu cortex-a8
	.eabi_attribute 6, 10
	.eabi_attribute 7, 65
	.eabi_attribute 8, 1
	.eabi_attribute 9, 2
	.fpu neon
	.eabi_attribute 10, 3
	.eabi_attribute 12, 1
	.eabi_attribute 20, 1
	.eabi_attribute 21, 1
	.eabi_attribute 23, 3
	.eabi_attribute 24, 1
	.eabi_attribute 25, 1
	.file	"basic-c-compile\\\\basic-c-compile.c"
	.text
	.globl	main
	.align	2
	.type	main,%function
main:
	.fnstart
	.save	{r11, lr}
	push	{r11, lr}
	ldr	r0, .LCPI0_0
	.setfp	r11, sp
	mov	r11, sp
	ldr	r1, .LCPI0_1
.LPC0_0:
	add	r0, pc, r0
	add	r0, r1, r0
	bl	puts(PLT)
	mov	r0, #0
	pop	{r11, pc}
	.align	2
.LCPI0_0:
	.long	_GLOBAL_OFFSET_TABLE_-(.LPC0_0+8)
.LCPI0_1:
	.long	.Lstr(GOTOFF)
.Ltmp0:
	.size	main, .Ltmp0-main
	.cantunwind
	.fnend

	.type	.Lstr,%object
	.section	.rodata.str1.1,"aMS",%progbits,1
.Lstr:
	.asciz	 "Hello World!"
	.size	.Lstr, 13


