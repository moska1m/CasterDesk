// SPDX-License-Identifier: GPL-2.0-or-later
#include "panel.hpp"
#include <QVBoxLayout>
#include <QScrollArea>
#include <QFile>
#include <QJsonDocument>
#include <QJsonArray>
#include <QNetworkReply>
#include <QNetworkProxy>
#include <QPointer>
#include <QRegularExpression>
#include <QPixmap>
#include <QSvgRenderer>
#include <QPainter>
#include <QUrl>
#include <windows.h>
#include <shlobj.h>
#include <cmath>

static void clearLayout(QLayout *layout)
{
    while (auto *item = layout->takeAt(0)) {
        if (item->widget()) item->widget()->deleteLater();
        if (item->layout()) clearLayout(item->layout());
        delete item;
    }
}

QString CasterDeskPanel::configurationPath()
{
    PWSTR documents = nullptr;
    if (FAILED(SHGetKnownFolderPath(FOLDERID_Documents, 0, nullptr, &documents))) return {};
    const QString path = QString::fromWCharArray(documents) + "/CasterDesk/Configuracoes/obs-connection.json";
    CoTaskMemFree(documents);
    return path;
}

CasterDeskPanel::CasterDeskPanel(QWidget *parent, QString path, bool autoPoll) : QWidget(parent), configPath(path.isEmpty() ? configurationPath() : path)
{
    setMinimumWidth(280);
    network.setProxy(QNetworkProxy::NoProxy);
    auto *root = new QVBoxLayout(this);
    auto *brand = new QLabel();QSvgRenderer logo(QStringLiteral(":/casterdesk/brand.svg"));logo.setAspectRatioMode(Qt::KeepAspectRatio);
    QPixmap mark(400,96);mark.setDevicePixelRatio(2);mark.fill(Qt::transparent);QPainter paint(&mark);logo.render(&paint,QRectF(0,0,200,48));paint.end();
    brand->setPixmap(mark);brand->setAlignment(Qt::AlignCenter);brand->setStyleSheet("background:#0a1118;padding:8px");brand->setAccessibleName("CasterDesk");root->addWidget(brand);
    status = new QLabel("Aguardando CasterDesk…"); status->setObjectName("connection"); status->setWordWrap(true); root->addWidget(status);
    context = new QLabel("—"); context->setWordWrap(true); root->addWidget(context);
    error = new QLabel(); error->setWordWrap(true); error->setStyleSheet("color:#ffb69d"); root->addWidget(error);
    tabs = new QTabWidget(); root->addWidget(tabs);
    auto *timerPage = new QWidget();timerPage->setObjectName("timer-page"); auto *timerLayout = new QVBoxLayout(timerPage);
    clock = new QLabel("00:00"); clock->setObjectName("time"); clock->setAlignment(Qt::AlignCenter); clock->setStyleSheet("font-size:38px"); timerLayout->addWidget(clock);
    auto *controls = new QGridLayout(); timerLayout->addLayout(controls);
    start = new QPushButton("▶ Iniciar / Retomar"); start->setObjectName("start");
    pause = new QPushButton("Ⅱ Pausar"); pause->setObjectName("pause");
    subtract = new QPushButton("− 1 min"); add = new QPushButton("+ 1 min");
    controls->addWidget(start,0,0); controls->addWidget(pause,0,1); controls->addWidget(subtract,1,0); controls->addWidget(add,1,1);
    auto *timeRow = new QHBoxLayout(); duration = new QLineEdit(); duration->setPlaceholderText("10:00"); duration->setObjectName("duration"); duration->setMaxLength(8);
    set = new QPushButton("Definir tempo"); set->setObjectName("set"); timeRow->addWidget(duration,1); timeRow->addWidget(set); timerLayout->addLayout(timeRow); timerLayout->addStretch();
    tabs->addTab(timerPage,"Cronômetro");
    auto *banPage = new QWidget();banPage->setObjectName("ban-page"); auto *banLayout = new QVBoxLayout(banPage); banSlots = new QGridLayout(); banLayout->addLayout(banSlots);
    phase = new QLabel(); phase->setWordWrap(true); banLayout->addWidget(phase);
    search = new QLineEdit(); search->setPlaceholderText("Filtrar operadores"); banLayout->addWidget(search);
    auto *scroll = new QScrollArea(); scroll->setWidgetResizable(true); auto *grid = new QWidget(); operators = new QGridLayout(grid); operators->setAlignment(Qt::AlignTop); scroll->setWidget(grid); banLayout->addWidget(scroll,1); tabs->addTab(banPage,"Bans de operadores");
    auto *roundPage=new QWidget();auto *roundLayout=new QVBoxLayout(roundPage);
    roundHeading=new QLabel();roundHeading->setWordWrap(true);roundLayout->addWidget(roundHeading);
    roundHistory=new QComboBox();roundHistory->setObjectName("round-history");roundLayout->addWidget(roundHistory);
    roundSite=new QComboBox();roundSite->setObjectName("round-site");roundLayout->addWidget(roundSite);
    auto *teamRow=new QHBoxLayout();roundLayout->addLayout(teamRow);
    for(int team=0;team<2;++team){roundTeams[team]=new QPushButton();roundTeams[team]->setObjectName(QString("round-team-%1").arg(team));roundTeams[team]->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);roundTeams[team]->setMinimumWidth(44);teamRow->addWidget(roundTeams[team]);connect(roundTeams[team],&QPushButton::clicked,this,[this,team]{roundWinner=team;updateRoundChoices();});}
    roundEnding=new QComboBox();roundEnding->setObjectName("round-ending");roundLayout->addWidget(roundEnding);
    roundSave=new QPushButton("Confirmar round");roundSave->setObjectName("round-save");roundLayout->addWidget(roundSave);roundLayout->addStretch();tabs->addTab(roundPage,"Round a round");tabs->removeTab(2);tabs->insertTab(0,roundPage,"Round a round");tabs->removeTab(1);tabs->addTab(timerPage,"Cronômetro");tabs->setCurrentIndex(0);
    for(auto *combo:{roundHistory,roundSite,roundEnding}){combo->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);combo->setMinimumWidth(60);}
    connect(roundHistory,qOverload<int>(&QComboBox::currentIndexChanged),this,[this]{if(!buildingRounds)renderRounds(true);});
    connect(roundSite,qOverload<int>(&QComboBox::currentIndexChanged),this,[this]{updateEnabled();});
    connect(roundEnding,qOverload<int>(&QComboBox::currentIndexChanged),this,[this]{updateEnabled();});
    connect(roundSave,&QPushButton::clicked,this,[this]{const auto match=state["match"].toObject();command({{"action","round"},{"matchId",match["id"]},{"mapId",match["mapId"]},{"revision",state["revision"]},{"site",roundSite->currentData().toString()},{"winner",roundWinner},{"ending",roundEnding->currentData().toString()},{"roundIndex",roundHistory->currentData().toInt()}});});
    connect(start,&QPushButton::clicked,this,[this]{command({{"action","start"}});});
    connect(pause,&QPushButton::clicked,this,[this]{command({{"action","pause"}});});
    connect(add,&QPushButton::clicked,this,[this]{command({{"action","adjust"},{"seconds",60}});});
    connect(subtract,&QPushButton::clicked,this,[this]{command({{"action","adjust"},{"seconds",-60}});});
    connect(set,&QPushButton::clicked,this,[this]{const int seconds=parseDuration(duration->text());if(seconds<0){error->setText(label("invalidTime","Use mm:ss ou hh:mm:ss."));return;}command({{"action","set"},{"seconds",seconds}});});
    connect(search,&QLineEdit::textChanged,this,[this]{renderOperators();});
    timer.setInterval(1000); connect(&timer,&QTimer::timeout,this,[this]{poll();});
    for(auto *text:findChildren<QLabel *>())text->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred);
    duration->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);duration->setMinimumWidth(60);
    search->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);search->setMinimumWidth(60);
    for(auto *button:{start,pause,add,subtract,set}){button->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);button->setMinimumWidth(44);}
    updateEnabled(); if(autoPoll){timer.start();QTimer::singleShot(0,this,[this]{poll();});}
}
CasterDeskPanel::~CasterDeskPanel()
{
    timer.stop();
    for(auto *reply:network.findChildren<QNetworkReply *>()){reply->disconnect(this);reply->abort();}
}

int CasterDeskPanel::parseDuration(const QString &text)
{
    if (!QRegularExpression("^\\d{1,2}:\\d{2}(:\\d{2})?$").match(text.trimmed()).hasMatch()) return -1;
    const auto parts=text.trimmed().split(':');const int last=parts.last().toInt();if(last>59)return -1;
    const int middle=parts[1].toInt();if(parts.size()==3&&middle>59)return -1;
    const int seconds=parts.size()==3?parts[0].toInt()*3600+middle*60+last:parts[0].toInt()*60+last;
    return seconds<=86400?seconds:-1;
}
QString CasterDeskPanel::label(const char *name,const char *fallback) const { return state["labels"].toObject()[name].toString(QString::fromUtf8(fallback)); }
void CasterDeskPanel::disconnectPanel(const QString &message) {connected=false;status->setText(message);context->setText("—");updateEnabled();}
bool CasterDeskPanel::discover()
{
    QFile file(configPath); if(!file.open(QIODevice::ReadOnly)){disconnectPanel("Abra o CasterDesk com a integração OBS instalada.");return false;}
    if(file.size()>8192){disconnectPanel("Configuração OBS inválida.");return false;}
    const auto config=QJsonDocument::fromJson(file.readAll()).object();const auto nextKey=config["Key"].toString();const int port=config["Port"].toInt();
    if(!config["Enabled"].toBool()){disconnectPanel("Ative a conexão OBS no CasterDesk.");return false;}
    if(port<1024||port>65535||!QRegularExpression("^[0-9a-fA-F]{64}$").match(nextKey).hasMatch()){disconnectPanel("Configuração OBS inválida.");return false;}
    const auto nextUrl=QString("http://127.0.0.1:%1").arg(port);
    if(nextKey!=key||nextUrl!=baseUrl){key=nextKey;baseUrl=nextUrl;icons.clear();rendered.clear();connected=false;}
    return true;
}
void CasterDeskPanel::poll() { if(!busy&&discover())request("/api/state"); }
void CasterDeskPanel::request(const QString &path,const QJsonObject *body)
{
    busy=true;if(body)updateEnabled();QNetworkRequest req{QUrl(baseUrl+path)};req.setRawHeader("X-CasterDesk-Key",key.toUtf8());req.setTransferTimeout(2500);req.setAttribute(QNetworkRequest::RedirectPolicyAttribute,QNetworkRequest::ManualRedirectPolicy);
    QNetworkReply *reply;
    if(body){req.setHeader(QNetworkRequest::ContentTypeHeader,"application/json");reply=network.post(req,QJsonDocument(*body).toJson(QJsonDocument::Compact));}
    else reply=network.get(req);
    const bool mutation=body!=nullptr;
    connect(reply,&QNetworkReply::finished,this,[this,reply,mutation]{busy=false;const int code=reply->attribute(QNetworkRequest::HttpStatusCodeAttribute).toInt();const auto data=QJsonDocument::fromJson(reply->readAll()).object();
        if(code==200&&data.contains("timer")&&data.contains("match"))receive(data);
        else if(mutation&&code>=400){error->setText(data["error"].toString("Comando recusado. Atualize o painel."));updateEnabled();}
        else disconnectPanel(code==401?"Conexão não autorizada. Aguardando nova configuração…":"Aguardando CasterDesk…");
        reply->deleteLater();});
}
void CasterDeskPanel::command(QJsonObject data)
{
    if(!connected||busy||!state["controllable"].toBool())return;
    error->clear();request("/api/command",&data);
}
void CasterDeskPanel::receive(const QJsonObject &data)
{
    state=data;connected=true;status->setText(label("connected","Conectado"));
    const auto match=data["match"].toObject();context->setText(match["teamA"].toString()+" × "+match["teamB"].toString()+" · "+match["map"].toString());
    const auto time=data["timer"].toObject();int seconds=static_cast<int>(std::ceil(time["seconds"].toDouble()));seconds=qBound(0,seconds,86400);const int hours=seconds/3600;
    clock->setText((hours>0?QString("%1:").arg(hours,2,10,QChar('0')):QString())+QString("%1:%2").arg((seconds/60)%60,2,10,QChar('0')).arg(seconds%60,2,10,QChar('0')));
    start->setText("▶");start->setToolTip(label("start","Iniciar / Retomar"));start->setAccessibleName(start->toolTip());pause->setText("Ⅱ");pause->setToolTip(label("pause","Pausar"));pause->setAccessibleName(pause->toolTip());set->setText(label("set","Definir tempo"));set->setToolTip(set->text());tabs->setTabText(2,label("timer","Cronômetro"));tabs->setTabText(1,label("bans","Bans de operadores"));search->setPlaceholderText(label("search","Filtrar operadores"));
    if(!data["controllable"].toBool())error->setText(label("busy","Conclua a edição aberta no CasterDesk."));else if(error->text()==label("busy","Conclua a edição aberta no CasterDesk."))error->clear();
    const QString next=match["id"].toString()+":"+match["mapId"].toString()+":"+QString::number(data["revision"].toInteger())+":"+QString::number(data["pending"].toInt(-1))+":"+data["role"].toString()+":"+data["language"].toString();
    if(next!=rendered){rendered=next;clearLayout(banSlots);int n=0;QString nextPhase=label("wait","Nenhum ban pendente.");
        for(const auto &value:data["bans"].toArray()){const auto ban=value.toObject();const auto team=ban["team"].toInt()==0?match["teamA"].toString():match["teamB"].toString();auto *slot=new QLabel((ban["name"].toString().isEmpty()?label(ban["unlocked"].toBool()?"select":"blocked", "—"):ban["name"].toString())+"\n"+team+" · "+ban["role"].toString());slot->setAlignment(Qt::AlignCenter);slot->setWordWrap(true);slot->setMinimumWidth(0);slot->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred);slot->setStyleSheet(QString("padding:5px;background:%1;border-bottom:3px solid %2;").arg(ban["team"].toInt()==0?"#305975":"#895333",ban["team"].toInt()==0?"#408fb3":"#ff7737"));banSlots->addWidget(slot,n/3,n%3);++n;if(!data["pending"].isNull()&&ban["slot"]==data["pending"])nextPhase=team+" · "+ban["role"].toString();}
        phase->setText(nextPhase);renderOperators();}
    renderRounds();updateEnabled();
}
void CasterDeskPanel::updateEnabled()
{
    const bool ready=connected&&!busy&&state["controllable"].toBool();const auto time=state["timer"].toObject();start->setEnabled(ready&&!time["running"].toBool()&&time["seconds"].toDouble()>0);pause->setEnabled(ready&&time["running"].toBool());add->setEnabled(ready);subtract->setEnabled(ready&&time["seconds"].toDouble()>0);set->setEnabled(ready);
    for(int n=0;n<operators->count();++n){auto *button=qobject_cast<QPushButton *>(operators->itemAt(n)->widget());if(button)button->setEnabled(ready&&button->property("available").toBool());}
    const auto rounds=state["rounds"].toObject();const bool roundReady=ready&&rounds["editable"].toBool()&&(roundHistory->currentData().toInt()>=0||rounds["ready"].toBool());
    roundHistory->setEnabled(ready&&rounds["editable"].toBool());roundSite->setEnabled(roundReady);roundEnding->setEnabled(roundReady&&roundWinner>=0);for(auto *team:roundTeams)team->setEnabled(roundReady);
    roundSave->setEnabled(roundReady&&roundWinner>=0&&!roundSite->currentData().toString().isEmpty()&&!roundEnding->currentData().toString().isEmpty());
}
void CasterDeskPanel::renderRounds(bool force)
{
    const auto data=state["rounds"].toObject();const auto match=state["match"].toObject();const QString token=match["id"].toString()+":"+match["mapId"].toString()+":"+QString::number(state["revision"].toInteger());
    if(token==roundsRendered&&!force)return;const bool sameMatch=roundsRendered.startsWith(match["id"].toString()+":"+match["mapId"].toString()+":");int edit=sameMatch?roundHistory->currentData().toInt():-1;
    roundsRendered=token;buildingRounds=true;roundHistory->clear();roundHistory->addItem(label("newRound","Novo round"),-1);
    const auto history=data["history"].toArray();for(const auto &value:history){const auto row=value.toObject();roundHistory->addItem(QString("%1 · %2").arg(row["index"].toInt()+1).arg(row["winner"].toInt()==0?match["teamA"].toString():match["teamB"].toString()),row["index"].toInt());}
    if(edit>=history.size())edit=-1;roundHistory->setCurrentIndex(edit+1);roundWinner=-1;roundSite->clear();roundSite->addItem(label("select","Selecionar…"),"");
    for(const auto &value:data["sites"].toArray()){const auto site=value.toObject();if(edit>=0||!site["blocked"].toBool())roundSite->addItem(site["label"].toString(),site["id"].toString());}
    if(edit>=0){const auto row=history[edit].toObject();roundWinner=row["winner"].toInt();roundSite->setCurrentIndex(roundSite->findData(row["site"].toString()));}
    tabs->setTabText(0,label("rounds","Round a round"));roundSave->setText(label(edit<0?"confirmRound":"editRound",edit<0?"Confirmar round":"Corrigir round"));
    roundHeading->setText(QString("Round %1 · %2").arg(edit<0?data["number"].toInt():edit+1).arg(data["score"].toString()));
    updateRoundChoices();if(edit>=0)roundEnding->setCurrentIndex(roundEnding->findData(history[edit].toObject()["ending"].toString()));buildingRounds=false;
    if(edit<0&&!data["ready"].toBool())roundHeading->setText(roundHeading->text()+"\n"+label("roundWait","Conclua os bans e os lados para continuar."));
}
void CasterDeskPanel::updateRoundChoices()
{
    const auto rounds=state["rounds"].toObject();const auto match=state["match"].toObject();int attacker=rounds["attacker"].toInt(-1);const int edit=roundHistory->currentData().toInt();const auto history=rounds["history"].toArray();if(edit>=0&&edit<history.size())attacker=history[edit].toObject()["attacker"].toInt();
    const auto old=roundEnding->currentData().toString();roundEnding->clear();roundEnding->addItem(label("select","Selecionar…"),"");
    if(roundWinner>=0)for(const auto &value:rounds["endings"].toArray()){const auto ending=value.toObject();if(ending[roundWinner==attacker?"attack":"defense"].toBool())roundEnding->addItem(ending["label"].toString(),ending["id"].toString());}
    const int previous=roundEnding->findData(old);if(previous>=0)roundEnding->setCurrentIndex(previous);
    for(int team=0;team<2;++team){roundTeams[team]->setText(match[team==0?"teamA":"teamB"].toString()+" · "+label(team==attacker?"atq":"def",team==attacker?"ATQ":"DEF"));roundTeams[team]->setToolTip(roundTeams[team]->text());roundTeams[team]->setStyleSheet(QString("background:%1;border:%2px solid white;").arg(team==0?"#305975":"#895333").arg(roundWinner==team?2:0));}
    updateEnabled();
}
void CasterDeskPanel::renderOperators()
{
    clearLayout(operators);int n=0;
    for(const auto &value:state["operators"].toArray()){const auto op=value.toObject();if(!op["name"].toString().contains(search->text().trimmed(),Qt::CaseInsensitive))continue;auto *button=new QPushButton(op["name"].toString());button->setObjectName("operator");button->setProperty("available",op["enabled"].toBool());button->setIconSize(QSize(32,32));button->setMinimumHeight(56);button->setMinimumWidth(0);button->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);button->setToolTip(op["name"].toString());operators->addWidget(button,n/3,n%3);++n;
        const auto match=state["match"].toObject();const QJsonObject payload{{"action","ban"},{"matchId",match["id"]},{"mapId",match["mapId"]},{"revision",state["revision"]},{"slot",state["pending"]},{"operatorId",op["id"]}};
        connect(button,&QPushButton::clicked,this,[this,payload]{command(payload);});loadIcon(op["id"].toString(),button);}
    updateEnabled();
}
void CasterDeskPanel::loadIcon(const QString &id,QPushButton *button)
{
    if(icons.contains(id)){button->setIcon(icons[id]);return;}
    QNetworkRequest req{QUrl(baseUrl+"/icons/"+QString::fromLatin1(QUrl::toPercentEncoding(id)))};req.setRawHeader("X-CasterDesk-Key",key.toUtf8());req.setTransferTimeout(2500);req.setAttribute(QNetworkRequest::RedirectPolicyAttribute,QNetworkRequest::ManualRedirectPolicy);
    auto *reply=network.get(req);QPointer<QPushButton> target=button;const auto connectionKey=key;
    connect(reply,&QNetworkReply::finished,this,[this,reply,target,id,connectionKey]{if(reply->error()==QNetworkReply::NoError&&key==connectionKey){QPixmap image;if(image.loadFromData(reply->readAll())){icons.insert(id,QIcon(image));if(target)target->setIcon(icons[id]);}}reply->deleteLater();});
}
