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
#include <QToolButton>
#include <QDoubleSpinBox>
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
    check(condition(),"condition completed within timeout");
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
        QJsonObject labels{{"connected","Connected"},{"disconnected","Disconnected"},{"timer","Timer"},{"bans","Bans"},{"start","Start"},{"pause","Pause"},{"set","Set"},{"busy","Finish editing"}};
        QJsonObject state{{"revision",0},{"language","en"},{"controllable",true},{"timer",QJsonObject{{"seconds",522},{"running",false}}},{"match",QJsonObject{{"id","demo"},{"mapId","clubhouse"},{"teamA","Blue"},{"teamB","Orange"},{"map","Clubhouse"}}},{"pending",0},{"role","DEF"},{"labels",labels},{"bans",QJsonArray{QJsonObject{{"slot",0},{"team",0},{"role","DEF"},{"name",""},{"unlocked",true}}}},{"operators",QJsonArray{QJsonObject{{"id","smoke"},{"name","Smoke"},{"enabled",true}}}}};
        auto initialTimer=state["timer"].toObject();initialTimer["options"]=QJsonObject{{"initial",600},{"format","mm:ss"},{"message",""},{"presets",QJsonArray{1,5,10}}};state["timer"]=initialTimer;
        state["rounds"]=QJsonObject{{"number",1},{"attacker",0},{"score","0 × 0"},{"editable",true},{"ready",false},{"sites",QJsonArray{QJsonObject{{"id","site-id"},{"label","Full bombsite name"},{"blocked",false}}}},{"endings",QJsonArray{QJsonObject{{"id","elimination"},{"label","Elimination"},{"attack",true},{"defense",true}}}},{"history",QJsonArray()}};
        QJsonArray banFixture;for(int slot=0;slot<12;++slot){int team=slot%6==0||slot%6==1||slot%6==4?0:1;banFixture.append(QJsonObject{{"slot",slot},{"team",team},{"role",team==(slot<6?0:1)?"DEF":"ATQ"},{"id",""},{"name",""},{"unlocked",slot<4}});}state["bans"]=banFixture;
        QJsonObject lastCommand;bool headerValid=false;
        QObject::connect(&server,&QTcpServer::newConnection,&app,[&]{while(server.hasPendingConnections()){auto *socket=server.nextPendingConnection();auto buffer=std::make_shared<QByteArray>();QObject::connect(socket,&QTcpSocket::readyRead,&app,[&,socket,buffer]{*buffer+=socket->readAll();const int boundary=buffer->indexOf("\r\n\r\n");if(boundary<0)return;const QByteArray headers=buffer->left(boundary);int length=0;for(const auto &line:headers.split('\n'))if(line.toLower().startsWith("content-length:"))length=line.mid(15).trimmed().toInt();if(buffer->size()<boundary+4+length)return;
            headerValid=headers.toLower().contains("x-casterdesk-key: "+QByteArray(64,'A').toLower());
            const bool mutation=headers.startsWith("POST");
            if(mutation){lastCommand=QJsonDocument::fromJson(buffer->mid(boundary+4,length)).object();auto clock=state["timer"].toObject();const auto action=lastCommand["action"].toString();if(action=="start")clock["running"]=true;if(action=="pause")clock["running"]=false;if(action=="set")clock["seconds"]=lastCommand["seconds"];if(action=="adjust")clock["seconds"]=qMax(0,clock["seconds"].toInt()+lastCommand["seconds"].toInt());state["timer"]=clock;if(action=="ban"){state["revision"]=1;state["pending"]=QJsonValue::Null;state["operators"]=QJsonArray();}}
            if(mutation){if(lastCommand["action"]=="selectMap"){auto match=state["match"].toObject();match["mapId"]=lastCommand["targetMapId"];state["match"]=match;state["revision"]=state["revision"].toInt()+1;}
                auto rounds=state["rounds"].toObject();if(lastCommand["action"]=="ban")rounds["ready"]=true;
                if(lastCommand["action"]=="round"){auto history=rounds["history"].toArray();const int index=lastCommand["roundIndex"].toInt(-1);QJsonObject row{{"index",index<0?history.size():index},{"site",lastCommand["site"]},{"winner",lastCommand["winner"]},{"ending",lastCommand["ending"]},{"attacker",0}};if(index<0)history.append(row);else history[index]=row;rounds["history"]=history;rounds["number"]=history.size()+1;state["revision"]=state["revision"].toInt()+1;}state["rounds"]=rounds;}
            auto matchSnapshot=state["match"].toObject();QJsonArray remakeMaps;auto mapList=matchSnapshot["maps"].toArray();if(mapList.isEmpty()&&!matchSnapshot["mapId"].toString().isEmpty())mapList.append(QJsonObject{{"id",matchSnapshot["mapId"]},{"label",matchSnapshot["map"]}});for(const auto &value:mapList){auto item=value.toObject();QJsonArray history;for(const auto &row:state["rounds"].toObject()["history"].toArray()){auto record=row.toObject();record["site"]="Full bombsite name";history.append(record);}item["initialAttack"]=0;item["otAttack"]=1;item["rounds"]=history;item["bans"]=QJsonArray();remakeMaps.append(item);}state["remakes"]=remakeMaps;
            const bool icon=headers.startsWith("GET /icons/");const QByteArray body=icon?QByteArray("{}") : QJsonDocument(state).toJson(QJsonDocument::Compact);
            socket->write(QByteArray(icon?"HTTP/1.1 404 Not Found\r\n":"HTTP/1.1 200 OK\r\n")+"Content-Type: application/json\r\nContent-Length: "+QByteArray::number(body.size())+"\r\nConnection: close\r\n\r\n"+body);socket->disconnectFromHost();QObject::disconnect(socket,&QTcpSocket::readyRead,nullptr,nullptr);});QObject::connect(socket,&QTcpSocket::disconnected,socket,&QObject::deleteLater);}});
        QTemporaryDir temp;const QString config=temp.path()+"/obs-connection.json";QFile file(config);check(file.open(QIODevice::WriteOnly),"config write");file.write(QJsonDocument(QJsonObject{{"Enabled",true},{"Port",port},{"Key",QString(64,'A')}}).toJson());file.close();
        CasterDeskPanel panel(nullptr,config,false);auto *status=panel.findChild<QLabel *>("connection");auto *clock=panel.findChild<QLabel *>("time");auto *start=panel.findChild<QPushButton *>("start");auto *pause=panel.findChild<QPushButton *>("pause");
        check(!start->isEnabled(),"offline disabled");panel.poll();waitFor([&]{return status->text()=="CONNECTED";});check(headerValid,"auth header");check(clock->text()=="08:42","time read");check(start->isEnabled(),"start ready");start->click();waitFor([&]{return pause->isEnabled();});check(lastCommand["action"]=="start","start command");pause->click();waitFor([&]{return start->isEnabled();});
        panel.findChild<QLineEdit *>("duration")->setText("02:30");panel.findChild<QPushButton *>("set")->click();waitFor([&]{return clock->text()=="02:30";});check(lastCommand["seconds"]==150,"set shared duration");
        auto *op=panel.findChild<QPushButton *>("operator");check(op&&op->isEnabled(),"operator ready");op->click();waitFor([&]{return lastCommand["action"]=="ban";});check(lastCommand["matchId"]=="demo"&&lastCommand["mapId"]=="clubhouse"&&lastCommand["revision"]==0&&lastCommand["slot"]==0,"ban context");waitFor([&]{return start->isEnabled();});
        auto *tabs=panel.findChild<QTabWidget *>();check(tabs->count()==3&&tabs->tabText(0)=="PARTIDA"&&tabs->tabText(1)=="REMAKE"&&tabs->tabText(2)=="TIMER","match remake timer tab order");
        auto *banGrid=panel.findChild<QGridLayout *>("ban-slots");int teams[2]={0,0};for(int n=0;n<banGrid->count();++n){auto *card=qobject_cast<QToolButton *>(banGrid->itemAt(n)->widget());if(!card)continue;int team=card->property("team").toInt(),row,col,rs,cs;banGrid->getItemPosition(n,&row,&col,&rs,&cs);check(row==team*2+1,"ban row preserves responsible team");++teams[team];}check(teams[0]==6&&teams[1]==6,"six icon cards per team including both halves");
        panel.findChild<QPushButton *>("round-site-0")->click();panel.findChild<QPushButton *>("round-team-0")->click();QApplication::processEvents();panel.findChild<QPushButton *>("round-ending-elimination")->click();QApplication::processEvents();
        auto *save=panel.findChild<QPushButton *>("round-save");check(save->isEnabled(),"complete round form ready");check(save->styleSheet().contains("#25834c"),"ready round confirmation is green");check(panel.findChild<QPushButton *>("round-site-0")->styleSheet().contains("#ff4335"),"selected site has red outline");save->click();waitFor([&]{return lastCommand["action"]=="round"&&start->isEnabled();});check(lastCommand["site"]=="site-id"&&lastCommand["winner"]==0&&lastCommand["ending"]=="elimination"&&lastCommand["roundIndex"]==-1,"new round command uses IDs and explicit selections");
        auto *history=panel.findChild<QTableWidget *>("round-history");check(history->rowCount()==0,"remake stays empty until explicit map selection");auto *remake=panel.findChild<QComboBox *>("remake-map");remake->setCurrentIndex(1);check(history->rowCount()==1&&history->columnCount()==3,"remake shows only round bombsite and winner");QMetaObject::invokeMethod(history,"cellDoubleClicked",Qt::DirectConnection,Q_ARG(int,0),Q_ARG(int,0));panel.findChild<QPushButton *>("round-team-1")->click();check(save->isEnabled(),"round review form ready");save->click();waitFor([&]{return lastCommand["roundIndex"]==0&&start->isEnabled();});check(state["rounds"].toObject()["history"].toArray().size()==1&&lastCommand["winner"]==1,"review edits existing round without adding another");
        auto match=state["match"].toObject();match["maps"]=QJsonArray{QJsonObject{{"id","clubhouse"},{"label","Clubhouse"}},QJsonObject{{"id","border"},{"label","Border"}}};state["match"]=match;panel.poll();auto *maps=panel.findChild<QComboBox *>("map-choice");waitFor([&]{return maps->count()==2&&maps->isEnabled();});maps->setCurrentIndex(1);waitFor([&]{return lastCommand["action"]=="selectMap"&&start->isEnabled();});check(lastCommand["targetMapId"]=="border"&&lastCommand["mapId"]=="clubhouse","series map selection sends stable map IDs");remake->setCurrentIndex(1);check(lastCommand["action"]=="selectMap"&&state["match"].toObject()["mapId"]=="border","remake browsing never changes active map");
        state["controllable"]=false;panel.poll();waitFor([&]{return !start->isEnabled()&&status->text()=="CONNECTED";});check(!panel.findChild<QPushButton *>("set")->isEnabled(),"modal guard");
        server.close();panel.poll();waitFor([&]{return status->text()=="DISCONNECTED";});check(!start->isEnabled(),"disconnected disabled");
        check(server.listen(QHostAddress::LocalHost,port),"restart server");state["controllable"]=true;state["match"]=QJsonObject{{"id","other"},{"teamA","Next"},{"teamB","Match"},{"map","Border"}};panel.poll();waitFor([&]{return status->text()=="CONNECTED";});check(start->isEnabled(),"automatic reconnect");check(remake->currentIndex()==0&&history->rowCount()==0,"new match clears explicit remake choice");check(panel.findChild<QLabel *>("remake-info")->isHidden(),"no stale remake on new match");
        auto longMatch=state["match"].toObject();longMatch["teamA"]="Long team "+QString(60,'A');longMatch["teamB"]="Long team "+QString(60,'B');state["match"]=longMatch;panel.poll();waitFor([&]{return panel.findChild<QLabel *>("match-context")->text().startsWith("LONG TEAM");});check(panel.findChild<QLabel *>("match-context")->alignment()==Qt::AlignCenter,"uppercase centered matchup");check(status->styleSheet().contains("#30d66a"),"connected badge is green");check(panel.findChild<QLineEdit *>("duration")->alignment()==Qt::AlignCenter,"timer input centered");
        panel.resize(320,1000);panel.show();QApplication::processEvents();
        check(panel.findChild<QLabel *>("connection-icon")!=nullptr,"connection uses approved microphone symbol");
        tabs->setCurrentIndex(1);QApplication::processEvents();check(remake->mapTo(tabs,QPoint(0,0)).y()<90,"empty remake selector stays at top of tall dock");tabs->setCurrentIndex(0);std::cout<<"Minimum dock width: "<<panel.minimumSizeHint().width()<<std::endl;
        for(auto *widget:panel.findChildren<QWidget *>())std::cout<<widget->metaObject()->className()<<" "<<widget->objectName().toStdString()<<" hint="<<widget->minimumSizeHint().width()<<" minimum="<<widget->minimumWidth()<<" policy="<<widget->sizePolicy().horizontalPolicy()<<std::endl;
        check(panel.minimumSizeHint().width()<=320,"compact width");
        check(status->width()>40,"connection badge remains readable in compact header");
        panel.findChild<QLineEdit *>("duration")->setText("04:00");panel.findChild<QLineEdit *>("final-message")->setText("MATCH STARTS NOW");panel.findChild<QComboBox *>("time-format")->setCurrentText("hh:mm:ss");panel.findChild<QPushButton *>("timer-config")->click();
        waitFor([&]{return lastCommand["action"]=="timerConfig"&&start->isEnabled();});check(lastCommand["seconds"]==240&&lastCommand["format"]=="hh:mm:ss"&&lastCommand["message"]=="MATCH STARTS NOW"&&lastCommand["presets"].toArray().size()==3,"complete timer configuration sends initial time format message and presets");
        for(int count:{0,1,8,3}) {
            auto clock=state["timer"].toObject();auto options=clock["options"].toObject();QJsonArray values;for(int i=0;i<count;++i)values.append(i+2);options["presets"]=values;clock["options"]=options;state["timer"]=clock;
            waitFor([&]{return panel.findChildren<QDoubleSpinBox *>().size()==count&&start->isEnabled();});
            lastCommand=QJsonObject();panel.findChild<QPushButton *>("timer-config")->click();
            waitFor([&]{return lastCommand["action"]=="timerConfig"&&start->isEnabled();});
            check(lastCommand["presets"].toArray()==values,"legacy preset count and values survive polling and configuration");
        }
        panel.findChild<QPushButton *>("reset")->click();waitFor([&]{return lastCommand["action"]=="reset"&&start->isEnabled();});check(lastCommand["action"]=="reset","reset targets shared countdown");
        std::cout<<"PASS: "<<checks<<" native Qt offscreen checks, authenticated HTTP, timer, ban context, modal guard and reconnect; OBS not opened.\n";return 0;
    }catch(const std::exception &error){std::cerr<<error.what()<<"\n";return 1;}
}
