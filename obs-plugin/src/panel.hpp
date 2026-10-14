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

class CasterDeskPanel final : public QWidget {
public:
    explicit CasterDeskPanel(QWidget *parent = nullptr, QString configPath = {}, bool autoPoll = true);
    ~CasterDeskPanel() override;
    void poll();
    static QString configurationPath();
    static int parseDuration(const QString &text);
private:
    QString configPath, key, baseUrl, rendered;
    QNetworkAccessManager network;
    QTimer timer;
    QJsonObject state;
    QHash<QString, QIcon> icons;
    QLabel *status, *context, *clock, *error, *phase;
    QTabWidget *tabs;
    QLineEdit *duration, *search;
    QGridLayout *banSlots, *operators;
    QPushButton *start, *pause, *add, *subtract, *set;
    bool connected = false, busy = false;
    QComboBox *roundHistory, *roundSite, *roundEnding;
    QPushButton *roundTeams[2], *roundSave;
    QLabel *roundHeading;
    int roundWinner = -1;
    bool buildingRounds = false;
    QString roundsRendered;
    void renderRounds(bool force = false);
    void updateRoundChoices();
    bool discover();
    void receive(const QJsonObject &data);
    void renderOperators();
    void updateEnabled();
    void command(QJsonObject data);
    void request(const QString &path, const QJsonObject *body = nullptr);
    void loadIcon(const QString &id, QPushButton *button);
    QString label(const char *name, const char *fallback) const;
    void disconnectPanel(const QString &message);
};
