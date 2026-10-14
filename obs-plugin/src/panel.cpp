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
#include <QHeaderView>
#include <QScrollBar>
#include <QClipboard>
#include <QApplication>
#include <windows.h>
#include <shlobj.h>
#include <cmath>

static void clearLayout(QLayout *layout)
{
    while (auto *item = layout->takeAt(0)) {
        if (item->widget()) { item->widget()->hide(); item->widget()->setObjectName({}); item->widget()->deleteLater(); }
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
    mapChoice=new QComboBox();mapChoice->setObjectName("map-choice");mapChoice->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);mapChoice->setMinimumWidth(60);root->addWidget(mapChoice);
    connect(mapChoice,qOverload<int>(&QComboBox::currentIndexChanged),this,[this]{if(buildingMaps||!connected)return;const auto match=state["match"].toObject();const auto target=mapChoice->currentData().toString();if(!target.isEmpty()&&target!=match["mapId"].toString())command({{"action","selectMap"},{"matchId",match["id"]},{"mapId",match["mapId"]},{"revision",state["revision"]},{"targetMapId",target}});});
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
    auto *timerScroll = new QScrollArea(); timerScroll->setWidgetResizable(true); timerScroll->setWidget(timerPage); tabs->addTab(timerScroll,"Cronômetro");
    auto *partida = new QWidget(); partida->setObjectName("match-page"); auto *partidaLayout = new QVBoxLayout(partida);
    auto *matchScroll = new QScrollArea(); matchScroll->setWidgetResizable(true); matchScroll->setWidget(partida);
    auto *roundPage = new QWidget(); auto *roundLayout = new QVBoxLayout(roundPage); partidaLayout->addWidget(roundPage);
    roundHeading = new QLabel(); roundHeading->setWordWrap(true); roundLayout->addWidget(roundHeading);
    roundSites = new QGridLayout(); roundLayout->addLayout(roundSites);
    auto *teamRow = new QHBoxLayout(); roundLayout->addLayout(teamRow);
    for(int team=0;team<2;++team){roundTeams[team]=new QPushButton();roundTeams[team]->setObjectName(QString("round-team-%1").arg(team));roundTeams[team]->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);roundTeams[team]->setMinimumWidth(44);roundTeams[team]->setFixedHeight(36);teamRow->addWidget(roundTeams[team]);connect(roundTeams[team],&QPushButton::clicked,this,[this,team]{roundWinner=team;updateRoundChoices();});}
    roundEndings = new QGridLayout(); roundLayout->addLayout(roundEndings);
    roundSave = new QPushButton("Confirmar round"); roundSave->setObjectName("round-save"); roundSave->setMinimumHeight(36); roundLayout->addWidget(roundSave);
    newRound = new QPushButton("Novo round"); newRound->setObjectName("new-round"); roundLayout->addWidget(newRound); newRound->hide();
    connect(newRound,&QPushButton::clicked,this,[this]{roundIndex=-1;renderRounds(true);});
    auto *historyTitle = new QLabel("Histórico de rounds"); historyTitle->setObjectName("history-title"); partidaLayout->addWidget(historyTitle);
    roundHistory = new QTableWidget(); roundHistory->setObjectName("round-history"); roundHistory->setColumnCount(7); roundHistory->setMinimumWidth(0); roundHistory->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred); roundHistory->setMinimumHeight(130); roundHistory->setMaximumHeight(220);
    roundHistory->horizontalHeader()->setSectionResizeMode(QHeaderView::Stretch); roundHistory->verticalHeader()->hide(); roundHistory->setEditTriggers(QAbstractItemView::NoEditTriggers); roundHistory->setSelectionBehavior(QAbstractItemView::SelectRows); roundHistory->setSelectionMode(QAbstractItemView::SingleSelection); partidaLayout->addWidget(roundHistory);
    connect(roundHistory,&QTableWidget::cellClicked,this,[this](int row,int){roundIndex=row;renderRounds(true);});
    auto *banPage = new QWidget(); banPage->setObjectName("ban-page"); auto *banLayout = new QVBoxLayout(banPage); partidaLayout->addWidget(banPage);
    auto *banTitle = new QLabel("Bans de operadores"); banTitle->setObjectName("ban-title"); banLayout->addWidget(banTitle);
    banSlots = new QGridLayout(); banLayout->addLayout(banSlots);
    phase = new QLabel(); phase->setWordWrap(true); banLayout->addWidget(phase);
    search = new QLineEdit(); search->setPlaceholderText("Filtrar operadores"); banLayout->addWidget(search);
    auto *grid = new QWidget(); operators = new QGridLayout(grid); operators->setAlignment(Qt::AlignTop); banLayout->addWidget(grid); partidaLayout->addStretch();
    tabs->insertTab(0,matchScroll,"Partida"); tabs->setCurrentIndex(0);
    connect(roundSave,&QPushButton::clicked,this,[this]{const auto match=state["match"].toObject();command({{"action","round"},{"matchId",match["id"]},{"mapId",match["mapId"]},{"revision",state["revision"]},{"site",roundSite},{"winner",roundWinner},{"ending",roundEnding},{"roundIndex",roundIndex}});});
    // All timer configuration operates on the same CasterDesk countdown and TXT.
    reset = new QPushButton("Resetar tempo"); reset->setObjectName("reset"); timerLayout->insertWidget(3,reset);
    timeFormat = new QComboBox(); timeFormat->addItems({"mm:ss","hh:mm:ss"}); timeFormat->setObjectName("time-format"); timerLayout->insertWidget(4,timeFormat);
    finalMessage = new QLineEdit(); finalMessage->setObjectName("final-message"); finalMessage->setMaxLength(500); finalMessage->setPlaceholderText("Mensagem final"); timerLayout->insertWidget(5,finalMessage);
    auto *presets = new QGridLayout(); timerLayout->insertLayout(6,presets);
    for(int i=0;i<3;++i){presetValues[i]=new QDoubleSpinBox();presetValues[i]->setRange(0.01,1440);presetValues[i]->setValue(i+2);presetValues[i]->setSuffix(" min");presetValues[i]->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);presetValues[i]->setMinimumWidth(44);presetButtons[i]=new QPushButton("+");presetButtons[i]->setObjectName(QString("preset-%1").arg(i));presets->addWidget(presetValues[i],i,0);presets->addWidget(presetButtons[i],i,1);connect(presetButtons[i],&QPushButton::clicked,this,[this,i]{command({{"action","adjust"},{"seconds",presetValues[i]->value()*60}});});}
    timerSave = new QPushButton("Aplicar configurações"); timerSave->setObjectName("timer-config"); timerLayout->insertWidget(7,timerSave);
    timerStatus = new QLabel(); timerStatus->setWordWrap(true); timerLayout->insertWidget(8,timerStatus);
    outputFolder = new QLineEdit(); outputFolder->setReadOnly(true); outputFolder->setObjectName("output-folder"); timerLayout->insertWidget(9,outputFolder);
    copyFolder = new QPushButton("Copiar caminho"); copyFolder->setObjectName("copy-folder"); timerLayout->insertWidget(10,copyFolder);
    connect(copyFolder,&QPushButton::clicked,this,[this]{QApplication::clipboard()->setText(outputFolder->text());});
    connect(reset,&QPushButton::clicked,this,[this]{command({{"action","reset"}});});
    connect(timerSave,&QPushButton::clicked,this,[this]{int seconds=parseDuration(duration->text());if(seconds<0){error->setText(label("invalidTime","Use mm:ss ou hh:mm:ss."));return;}QJsonArray presets;for(auto *value:presetValues)presets.append(value->value());command({{"action","timerConfig"},{"seconds",seconds},{"format",timeFormat->currentText()},{"message",finalMessage->text()},{"presets",presets}});});
    connect(start,&QPushButton::clicked,this,[this]{command({{"action","start"}});});
    connect(pause,&QPushButton::clicked,this,[this]{command({{"action","pause"}});});
    connect(add,&QPushButton::clicked,this,[this]{command({{"action","adjust"},{"seconds",60}});});
    connect(subtract,&QPushButton::clicked,this,[this]{command({{"action","adjust"},{"seconds",-60}});});
    connect(set,&QPushButton::clicked,this,[this]{const int seconds=parseDuration(duration->text());if(seconds<0){error->setText(label("invalidTime","Use mm:ss ou hh:mm:ss."));return;}command({{"action","set"},{"seconds",seconds}});});
    connect(search,&QLineEdit::textChanged,this,[this]{renderOperators();});
    timer.setInterval(1000); connect(&timer,&QTimer::timeout,this,[this]{poll();});
    for(auto *page:{timerPage,banPage,roundPage,partida})page->ensurePolished();
    for(auto *text:findChildren<QLabel *>())text->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred);
    duration->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);duration->setMinimumWidth(60);
    search->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);search->setMinimumWidth(60);
    for(auto *button:{start,pause,add,subtract,set}){button->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);button->setMinimumWidth(44);}
    for(auto *page:{timerPage,banPage,roundPage,partida}){page->layout()->invalidate();page->layout()->activate();}
    updateEnabled(); if(autoPoll){timer.start();QTimer::singleShot(0,this,[this]{poll();});}
}
void CasterDeskPanel::resizeEvent(QResizeEvent *event)
{
    QWidget::resizeEvent(event);
    // Narrow docks retain result/site/ending; full-width docks also show both sides.
    if(roundHistory){roundHistory->setColumnHidden(1,width()<520);roundHistory->setColumnHidden(2,width()<520);}
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
    const auto previousPending=state["pending"];
    state=data;connected=true;status->setText(label("connected","Conectado"));
    const auto match=data["match"].toObject();context->setText(match["teamA"].toString()+" × "+match["teamB"].toString()+" · "+match["map"].toString());
    const auto mapsToken=match["id"].toString()+":"+match["mapId"].toString()+":"+QString::fromUtf8(QJsonDocument(match["maps"].toArray()).toJson(QJsonDocument::Compact));
    if(mapsToken!=mapsRendered){mapsRendered=mapsToken;buildingMaps=true;mapChoice->clear();for(const auto &value:match["maps"].toArray()){const auto map=value.toObject();mapChoice->addItem(map["label"].toString(),map["id"].toString());}mapChoice->setCurrentIndex(mapChoice->findData(match["mapId"].toString()));mapChoice->setVisible(mapChoice->count()>1);buildingMaps=false;}
    const auto time=data["timer"].toObject();int seconds=static_cast<int>(std::ceil(time["seconds"].toDouble()));seconds=qBound(0,seconds,86400);const int hours=seconds/3600;
    clock->setText((hours>0?QString("%1:").arg(hours,2,10,QChar('0')):QString())+QString("%1:%2").arg((seconds/60)%60,2,10,QChar('0')).arg(seconds%60,2,10,QChar('0')));
    start->setText(label("start","Iniciar / Retomar"));start->setToolTip(start->text());start->setAccessibleName(start->toolTip());pause->setText(label("pause","Pausar"));pause->setToolTip(pause->text());pause->setAccessibleName(pause->toolTip());set->setText(label("set","Definir tempo"));set->setToolTip(set->text());tabs->setTabText(1,label("timer","Cronômetro"));tabs->setTabText(0,label("partida","Partida"));findChild<QLabel *>("ban-title")->setText(label("bans","Bans de operadores"));findChild<QLabel *>("history-title")->setText(label("history","Histórico de rounds"));search->setPlaceholderText(label("search","Filtrar operadores"));
    if(!data["controllable"].toBool())error->setText(label("busy","Conclua a edição aberta no CasterDesk."));else if(error->text()==label("busy","Conclua a edição aberta no CasterDesk."))error->clear();
    const QString next=match["id"].toString()+":"+match["mapId"].toString()+":"+QString::number(data["revision"].toInteger())+":"+QString::number(data["pending"].toInt(-1))+":"+data["role"].toString()+":"+data["language"].toString();
    if(next!=rendered){rendered=next;clearLayout(banSlots);int n=0;QString nextPhase=label("wait","Nenhum ban pendente.");
        for(const auto &value:data["bans"].toArray()){const auto ban=value.toObject();const auto team=ban["team"].toInt()==0?match["teamA"].toString():match["teamB"].toString();auto *slot=new QLabel((ban["name"].toString().isEmpty()?label(ban["unlocked"].toBool()?"select":"blocked", "—"):ban["name"].toString())+"\n"+team+" · "+ban["role"].toString());slot->setAlignment(Qt::AlignCenter);slot->setWordWrap(true);slot->setMinimumWidth(0);slot->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred);slot->setStyleSheet(QString("padding:5px;background:%1;border-bottom:3px solid %2;").arg(ban["team"].toInt()==0?"#305975":"#895333",ban["team"].toInt()==0?"#408fb3":"#ff7737"));banSlots->addWidget(slot,n/3,n%3);++n;if(!data["pending"].isNull()&&ban["slot"]==data["pending"])nextPhase=team+" · "+ban["role"].toString();}
        phase->setText(nextPhase);renderOperators();}
    renderRounds();updateEnabled();
    Q_UNUSED(previousPending);
    const auto options=time["options"].toObject();
    // Polling must not overwrite text or preset values while the caster is typing.
    const auto settingsToken=QString::number(options["initial"].toDouble())+options["format"].toString()+options["message"].toString()+QString::fromUtf8(QJsonDocument(options["presets"].toArray()).toJson());
    if(settingsToken!=timerOptionsRendered){timerOptionsRendered=settingsToken;if(!duration->hasFocus()){int initial=options["initial"].toInt(600);duration->setText(QString("%1:%2:%3").arg(initial/3600,2,10,QChar('0')).arg(initial/60%60,2,10,QChar('0')).arg(initial%60,2,10,QChar('0')));}if(!finalMessage->hasFocus())finalMessage->setText(options["message"].toString());if(!timeFormat->hasFocus())timeFormat->setCurrentText(options["format"].toString("mm:ss"));auto presets=options["presets"].toArray();for(int i=0;i<3&&i<presets.size();++i)if(!presetValues[i]->hasFocus())presetValues[i]->setValue(presets[i].toDouble());}
    timerStatus->setText(options["status"].toString());outputFolder->setText(options["folder"].toString());outputFolder->setToolTip(options["output"].toString());
    if(!options["readout"].toString().isEmpty())clock->setText(options["readout"].toString());
    reset->setText(label("reset","Resetar tempo"));timerSave->setText(label("applyTimer","Aplicar configurações"));copyFolder->setText(label("copy","Copiar caminho"));finalMessage->setPlaceholderText(label("message","Mensagem final:"));
    for(int i=0;i<3;++i)presetButtons[i]->setText(QString("+ %1 min").arg(presetValues[i]->value()));

}
void CasterDeskPanel::updateEnabled()
{
    const bool ready=connected&&!busy&&state["controllable"].toBool();const auto time=state["timer"].toObject();start->setEnabled(ready&&!time["running"].toBool()&&time["seconds"].toDouble()>0);pause->setEnabled(ready&&time["running"].toBool());add->setEnabled(ready);subtract->setEnabled(ready&&time["seconds"].toDouble()>0);set->setEnabled(ready);
    mapChoice->setEnabled(ready&&mapChoice->count()>1);
    for(int n=0;n<operators->count();++n){auto *button=qobject_cast<QPushButton *>(operators->itemAt(n)->widget());if(button)button->setEnabled(ready&&button->property("available").toBool());}
    const auto rounds=state["rounds"].toObject();const bool roundReady=ready&&rounds["editable"].toBool()&&(roundIndex>=0||rounds["ready"].toBool());
    roundHistory->setEnabled(ready&&rounds["editable"].toBool());newRound->setEnabled(ready);
    for(int n=0;n<roundSites->count();++n){auto *button=qobject_cast<QPushButton *>(roundSites->itemAt(n)->widget());if(button)button->setEnabled(roundReady&&button->property("available").toBool());}
    for(int n=0;n<roundEndings->count();++n){auto *button=qobject_cast<QPushButton *>(roundEndings->itemAt(n)->widget());if(button)button->setEnabled(roundReady&&roundWinner>=0&&button->property("available").toBool());}
    for(auto *team:roundTeams)team->setEnabled(roundReady);
    roundSave->setEnabled(roundReady&&roundWinner>=0&&!roundSite.isEmpty()&&!roundEnding.isEmpty());
    for(auto *button:{reset,timerSave})button->setEnabled(ready);
    for(int i=0;i<3;++i){presetValues[i]->setEnabled(ready);presetButtons[i]->setEnabled(ready);}
    finalMessage->setEnabled(ready);timeFormat->setEnabled(ready);copyFolder->setEnabled(connected&&!outputFolder->text().isEmpty());
}
static QPushButton *choiceButton(const QString &text,const QString &name)
{
    auto *button=new QPushButton();button->setObjectName(name);button->setToolTip(text);button->setAccessibleName(text);button->setMinimumWidth(0);button->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Fixed);button->setFixedHeight(48);
    auto *box=new QVBoxLayout(button);box->setContentsMargins(4,2,4,2);auto *caption=new QLabel(text);caption->setWordWrap(true);caption->setAlignment(Qt::AlignCenter);caption->setMinimumWidth(0);caption->setSizePolicy(QSizePolicy::Ignored,QSizePolicy::Preferred);caption->setAttribute(Qt::WA_TransparentForMouseEvents);box->addWidget(caption);return button;
}
void CasterDeskPanel::renderRounds(bool force)
{
    const auto data=state["rounds"].toObject();const auto match=state["match"].toObject();const QString prefix=match["id"].toString()+":"+match["mapId"].toString()+":";const QString token=prefix+QString::number(state["revision"].toInteger())+":"+state["language"].toString();
    if(token==roundsRendered&&!force)return;const bool sameMatch=roundsRendered.startsWith(prefix);if(!sameMatch)roundIndex=-1;
    const auto history=data["history"].toArray();if(roundIndex>=history.size())roundIndex=-1;
    const bool follow=!sameMatch||history.size()>roundHistory->rowCount();roundsRendered=token;roundWinner=-1;roundSite.clear();roundEnding.clear();
    if(roundIndex>=0){const auto row=history[roundIndex].toObject();roundWinner=row["winner"].toInt();roundSite=row["site"].toString();roundEnding=row["ending"].toString();}
    clearLayout(roundSites);int n=0;
    for(const auto &value:data["sites"].toArray()){const auto site=value.toObject();auto *button=choiceButton(site["label"].toString(),QString("round-site-%1").arg(n));button->setProperty("site",site["id"]);button->setProperty("available",roundIndex>=0||!site["blocked"].toBool());roundSites->addWidget(button,n/2,n%2);++n;connect(button,&QPushButton::clicked,this,[this,button]{roundSite=button->property("site").toString();updateRoundChoices();});}
    roundHistory->setHorizontalHeaderLabels({label("round","Round"),label("atq","ATQ"),label("def","DEF"),label("site","Bomb site"),label("winner","Vencedor"),label("score","Placar"),label("ending","Finalização")});roundHistory->setRowCount(history.size());int a=0,b=0;
    for(int row=0;row<history.size();++row){const auto r=history[row].toObject();const int team=r["winner"].toInt();team==0?++a:++b;QString siteText=r["site"].toString(),endText=r["ending"].toString();for(const auto &value:data["sites"].toArray())if(value.toObject()["id"]==r["site"])siteText=value.toObject()["label"].toString();for(const auto &value:data["endings"].toArray())if(value.toObject()["id"]==r["ending"])endText=value.toObject()["label"].toString();const int attacker=r["attacker"].toInt();QStringList values{QString::number(row+1),match[attacker==0?"teamA":"teamB"].toString(),match[attacker==0?"teamB":"teamA"].toString(),siteText,match[team==0?"teamA":"teamB"].toString(),QString("%1–%2").arg(a).arg(b),endText};for(int col=0;col<7;++col){auto *item=new QTableWidgetItem(values[col]);item->setToolTip(values[col]);item->setTextAlignment(Qt::AlignCenter);item->setBackground(QColor(team==0?"#305975":"#895333"));item->setForeground(Qt::white);roundHistory->setItem(row,col,item);}roundHistory->setRowHeight(row,30);}
    if(follow)roundHistory->scrollToBottom();
    roundSave->setText(label(roundIndex<0?"confirmRound":"editRound",roundIndex<0?"Confirmar round":"Corrigir round"));newRound->setVisible(roundIndex>=0);newRound->setText(label("newRound","Novo round"));
    int attacker=data["attacker"].toInt(-1);if(roundIndex>=0)attacker=history[roundIndex].toObject()["attacker"].toInt();
    roundHeading->setText(label("rounds","Round a round")+" · "+QString("%1 · %2 %3 / %4 %5 · %6").arg(roundIndex<0?data["number"].toInt():roundIndex+1).arg(match[attacker==0?"teamA":"teamB"].toString(),label("atq","ATQ"),match[attacker==0?"teamB":"teamA"].toString(),label("def","DEF"),data["score"].toString()));
    if(roundIndex<0&&!data["ready"].toBool())roundHeading->setText(roundHeading->text()+"\n"+label("roundWait","Conclua os bans e os lados para continuar."));
    updateRoundChoices();
}
void CasterDeskPanel::updateRoundChoices()
{
    const auto rounds=state["rounds"].toObject();const auto match=state["match"].toObject();int attacker=rounds["attacker"].toInt(-1);const auto history=rounds["history"].toArray();if(roundIndex>=0&&roundIndex<history.size())attacker=history[roundIndex].toObject()["attacker"].toInt();
    bool allowed=false;for(const auto &value:rounds["endings"].toArray()){const auto end=value.toObject();if(end["id"].toString()==roundEnding&&end[roundWinner==attacker?"attack":"defense"].toBool())allowed=true;}if(roundWinner<0||!allowed)roundEnding.clear();
    clearLayout(roundEndings);int n=0;for(const auto &value:rounds["endings"].toArray()){const auto end=value.toObject();auto *button=choiceButton(end["label"].toString(),"round-ending-"+end["id"].toString());button->setFixedHeight(38);button->setProperty("available",end[roundWinner==attacker?"attack":"defense"].toBool());button->setStyleSheet(end["id"].toString()==roundEnding?"border:2px solid #72c8ef":"");roundEndings->addWidget(button,n/2,n%2);++n;connect(button,&QPushButton::clicked,this,[this,end]{roundEnding=end["id"].toString();updateRoundChoices();});}
    for(int i=0;i<roundSites->count();++i){auto *button=qobject_cast<QPushButton *>(roundSites->itemAt(i)->widget());button->setStyleSheet(button->property("site").toString()==roundSite?"border:2px solid #72c8ef":"");}
    for(int team=0;team<2;++team){roundTeams[team]->setText(match[team==0?"teamA":"teamB"].toString());roundTeams[team]->setToolTip(roundTeams[team]->text()+" · "+label(team==attacker?"atq":"def",team==attacker?"ATQ":"DEF"));roundTeams[team]->setStyleSheet(QString("background:%1;border:%2px solid white;").arg(team==0?"#305975":"#895333").arg(roundWinner==team?2:0));}
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
