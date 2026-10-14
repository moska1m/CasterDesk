// SPDX-License-Identifier: GPL-2.0-or-later
#include "panel.hpp"
#include <QApplication>
#include <QTcpServer>
#include <QTcpSocket>
#include <QHostAddress>
#include <QTemporaryDir>
#include <QFile>
#include <QJsonDocument>
#include <QJsonArray>
#include <QElapsedTimer>
#include <QThread>
#include <functional>
#include <memory>
#include <stdexcept>
#include <iostream>

static int checks=0;
static void check(bool condition,const char *message){if(!condition)throw std::runtime_error(message);++checks;std::cout<<"PASS "<<message<<std::endl;}
static void waitFor(const std::function<bool()> &condition)
{
    QElapsedTimer elapsed;elapsed.start();
    while(!condition()&&elapsed.elapsed()<6000){QApplication::processEvents();QThread::msleep(5);}
    check(condition(),"condition timed out");
}
int main(int argc,char **argv)
{
    QApplication app(argc,argv);
    try {
        check(CasterDeskPanel::parseDuration("02:30")==150,"parse mm:ss");
        check(CasterDeskPanel::parseDuration("01:02:03")==3723,"parse hh:mm:ss");
        check(CasterDeskPanel::parseDuration("02:60")==-1,"reject seconds");
        check(CasterDeskPanel::parseDuration("99:00:00")==-1,"reject over limit");
        check(CasterDeskPanel::parseDuration("bad")==-1,"reject malformed");
        check(!CasterDeskPanel::configurationPath().isEmpty(),"Windows Documents discovery");
        QTcpServer server;check(server.listen(QHostAddress::LocalHost,0),"listen");const auto port=server.serverPort();
        QJsonObject labels{{"connected","Connected"},{"timer","Timer"},{"bans","Bans"},{"start","Start"},{"pause","Pause"},{"set","Set"},{"busy","Finish editing"}};
        QJsonObject state{{"revision",0},{"language","en"},{"controllable",true},{"timer",QJsonObject{{"seconds",522},{"running",false}}},{"match",QJsonObject{{"id","demo"},{"mapId","clubhouse"},{"teamA","Blue"},{"teamB","Orange"},{"map","Clubhouse"}}},{"pending",0},{"role","DEF"},{"labels",labels},{"bans",QJsonArray{QJsonObject{{"slot",0},{"team",0},{"role","DEF"},{"name",""},{"unlocked",true}}}},{"operators",QJsonArray{QJsonObject{{"id","smoke"},{"name","Smoke"},{"enabled",true}}}}};
        QJsonObject lastCommand;bool headerValid=false;
        QObject::connect(&server,&QTcpServer::newConnection,&app,[&]{while(server.hasPendingConnections()){auto *socket=server.nextPendingConnection();auto buffer=std::make_shared<QByteArray>();QObject::connect(socket,&QTcpSocket::readyRead,&app,[&,socket,buffer]{*buffer+=socket->readAll();const int boundary=buffer->indexOf("\r\n\r\n");if(boundary<0)return;const QByteArray headers=buffer->left(boundary);int length=0;for(const auto &line:headers.split('\n'))if(line.toLower().startsWith("content-length:"))length=line.mid(15).trimmed().toInt();if(buffer->size()<boundary+4+length)return;
            headerValid=headers.toLower().contains("x-casterdesk-key: "+QByteArray(64,'A').toLower());
            const bool mutation=headers.startsWith("POST");
            if(mutation){lastCommand=QJsonDocument::fromJson(buffer->mid(boundary+4,length)).object();auto clock=state["timer"].toObject();const auto action=lastCommand["action"].toString();if(action=="start")clock["running"]=true;if(action=="pause")clock["running"]=false;if(action=="set")clock["seconds"]=lastCommand["seconds"];if(action=="adjust")clock["seconds"]=qMax(0,clock["seconds"].toInt()+lastCommand["seconds"].toInt());state["timer"]=clock;if(action=="ban"){state["revision"]=1;state["pending"]=QJsonValue::Null;state["operators"]=QJsonArray();}}
            const bool icon=headers.startsWith("GET /icons/");const QByteArray body=icon?QByteArray("{}") : QJsonDocument(state).toJson(QJsonDocument::Compact);
            socket->write(QByteArray(icon?"HTTP/1.1 404 Not Found\r\n":"HTTP/1.1 200 OK\r\n")+"Content-Type: application/json\r\nContent-Length: "+QByteArray::number(body.size())+"\r\nConnection: close\r\n\r\n"+body);socket->disconnectFromHost();QObject::disconnect(socket,&QTcpSocket::readyRead,nullptr,nullptr);});QObject::connect(socket,&QTcpSocket::disconnected,socket,&QObject::deleteLater);}});
        QTemporaryDir temp;const QString config=temp.path()+"/obs-connection.json";QFile file(config);check(file.open(QIODevice::WriteOnly),"config write");file.write(QJsonDocument(QJsonObject{{"Enabled",true},{"Port",port},{"Key",QString(64,'A')}}).toJson());file.close();
        CasterDeskPanel panel(nullptr,config,false);auto *status=panel.findChild<QLabel *>("connection");auto *clock=panel.findChild<QLabel *>("time");auto *start=panel.findChild<QPushButton *>("start");auto *pause=panel.findChild<QPushButton *>("pause");
        check(!start->isEnabled(),"offline disabled");panel.poll();waitFor([&]{return status->text()=="Connected";});check(headerValid,"auth header");check(clock->text()=="08:42","time read");check(start->isEnabled(),"start ready");start->click();waitFor([&]{return pause->isEnabled();});check(lastCommand["action"]=="start","start command");pause->click();waitFor([&]{return start->isEnabled();});
        panel.findChild<QLineEdit *>("duration")->setText("02:30");panel.findChild<QPushButton *>("set")->click();waitFor([&]{return clock->text()=="02:30";});check(lastCommand["seconds"]==150,"set shared duration");
        auto *op=panel.findChild<QPushButton *>("operator");check(op&&op->isEnabled(),"operator ready");op->click();waitFor([&]{return lastCommand["action"]=="ban";});check(lastCommand["matchId"]=="demo"&&lastCommand["mapId"]=="clubhouse"&&lastCommand["revision"]==0&&lastCommand["slot"]==0,"ban context");waitFor([&]{return start->isEnabled();});
        state["controllable"]=false;panel.poll();waitFor([&]{return !start->isEnabled()&&status->text()=="Connected";});check(!panel.findChild<QPushButton *>("set")->isEnabled(),"modal guard");
        server.close();panel.poll();waitFor([&]{return status->text().contains("Aguardando");});check(!start->isEnabled(),"disconnected disabled");
        check(server.listen(QHostAddress::LocalHost,port),"restart server");state["controllable"]=true;state["match"]=QJsonObject{{"id","other"},{"teamA","Next"},{"teamB","Match"},{"map","Border"}};panel.poll();waitFor([&]{return status->text()=="Connected";});check(start->isEnabled(),"automatic reconnect");
        panel.resize(320,700);panel.ensurePolished();std::cout<<"Minimum dock width: "<<panel.minimumSizeHint().width()<<std::endl;check(panel.minimumSizeHint().width()<=320,"compact width");
        std::cout<<"PASS: "<<checks<<" native Qt offscreen checks, authenticated HTTP, timer, ban context, modal guard and reconnect; OBS not opened.\n";return 0;
    }catch(const std::exception &error){std::cerr<<error.what()<<"\n";return 1;}
}
