// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#include <QWidget>
#include <QJsonObject>
#include <QNetworkAccessManager>
#include <QTimer>
#include <QLabel>
#include <QPushButton>
#include <QLineEdit>
#include <QGridLayout>
#include <QTabWidget>
#include <QHash>
#include <QIcon>
#include <QComboBox>
#include <QTableWidget>
#include <QDoubleSpinBox>
#include <QAbstractButton>
#include <QVBoxLayout>
#include <QVector>

class CasterDeskPanel final : public QWidget {
public:
    explicit CasterDeskPanel(QWidget *parent = nullptr, QString configPath = {}, bool autoPoll = true);
    ~CasterDeskPanel() override;
    void poll();
    static QString configurationPath();
    static int parseDuration(const QString &text);
protected:
    void resizeEvent(QResizeEvent *event) override;
private:
    QString configPath, key, baseUrl, rendered;
    QNetworkAccessManager network;
    QTimer timer;
    QJsonObject state;
    QHash<QString, QIcon> icons;
    QLabel *connectionIcon;
    void paintConnection();
    QLabel *status, *context, *clock, *error, *phase, *mapLabel;
    QTabWidget *tabs;
    QLineEdit *duration, *search;
    QGridLayout *banSlots, *operators;
    QPushButton *start, *pause, *add, *subtract, *set;
    bool connected = false, busy = false;
    QGridLayout *roundSites, *roundEndings;
    QTableWidget *roundHistory = nullptr;
    QPushButton *roundTeams[2], *roundSave;
    QLabel *roundHeading;
    int roundWinner = -1;
    int roundIndex = -1;
    QString roundSite, roundEnding;
    QPushButton *newRound;
    bool buildingRounds = false;
    QString roundsRendered;
    QComboBox *mapChoice;
    bool buildingMaps = false;
    QString mapsRendered;
    QLineEdit *finalMessage, *outputFolder;
    QComboBox *timeFormat;
    QVector<QDoubleSpinBox *> presetValues;
    QVector<QPushButton *> presetButtons;
    QGridLayout *presetGrid;
    QPushButton *timerSave, *reset, *copyFolder;
    QString timerOptionsRendered;
    QComboBox *remakeChoice;
    QLabel *remakeInfo, *remakeCheck;
    QVBoxLayout *remakeBans;
    QString remakeMatch;
    QString remakeOptionsRendered, remakeContentRendered;
    bool buildingRemake = false;
    void renderRemake();
    QLabel *timerStatus;
    void renderRounds(bool force = false);
    void updateRoundChoices();
    bool discover();
    void receive(const QJsonObject &data);
    void renderOperators();
    void updateEnabled();
    void command(QJsonObject data);
    void request(const QString &path, const QJsonObject *body = nullptr);
    void loadIcon(const QString &id, QAbstractButton *button);
    QString label(const char *name, const char *fallback) const;
    void disconnectPanel(const QString &message);
};
