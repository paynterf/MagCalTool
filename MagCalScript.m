
% main routine : calls MgnCalibration
%                        and an auxilliary function 
%                        which performs 3D rotations
%
%
%   Original code by Alain Barraud  2008
%   Modified for real data by Mitch 2010
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
close all;%close previously open figures
%clear
%load magdata.txt

%data=magdata';
data=S';

n=length(data(1,:))


[A,c] = MgnCalibration(data); %call routine

%A
%c

%plot corrected data
Caldata=A*(data-repmat(c,1,n));% calibrated data


%sample point w = A*(v-c)

v(1)=131.000;
v(2)=167.000;
v(3)=-377.000;
v
c
temp=v'-c
w=A*temp
