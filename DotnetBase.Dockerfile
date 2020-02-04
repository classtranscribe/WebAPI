FROM ubuntu:18.04 AS ubuntu_asp_base
RUN apt-get -y update
RUN apt-get install -y wget
RUN apt-get install -y software-properties-common
RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get -y update
RUN add-apt-repository universe
RUN apt-get -y update
RUN apt-get install -y apt-transport-https
RUN apt-get install -y liblttng-ust0 libcurl3 libssl1.0.0 libkrb5-3 zlib1g libicu60 libasound2
RUN apt-get -y update
ENV BIN_PATH "/usr/bin"
RUN apt-get install -y screen
ENV DOTNET_ROOT $BIN_PATH/dotnet
ENV PATH $PATH:$BIN_PATH/dotnet/
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install -y openssh-server
RUN mkdir /var/run/sshd
RUN echo 'root:JohnWick123!' | chpasswd
RUN sed -i 's/PermitRootLogin prohibit-password/PermitRootLogin yes/' /etc/ssh/sshd_config

# SSH login fix. Otherwise user is kicked off after login
RUN sed 's@session\s*required\s*pam_loginuid.so@session optional pam_loginuid.so@g' -i /etc/pam.d/sshd

ENV NOTVISIBLE "in users profile"
RUN echo "export VISIBLE=now" >> /etc/profile

EXPOSE 22
CMD ["/usr/sbin/sshd", "-D"]

