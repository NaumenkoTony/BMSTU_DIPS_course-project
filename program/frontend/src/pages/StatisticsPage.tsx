import { useEffect, useState } from "react";
import {
  fetchStatisticsSummary,
  fetchRecentActions,
  type StatisticsSummary,
  type UserAction,
} from "../api/StatisticsClient";
import {
  Card,
  Modal, 
  Box, 
  Container,
  TextInput,
  Group,
  Text,
  Pagination,
  Badge,
  CopyButton, 
  ActionIcon, 
  Tooltip,
  SimpleGrid
} from "@mantine/core";
import { IconHistory, IconUser, IconActivity, IconCheck, IconCopy, IconCrown, IconTrendingUp } from "@tabler/icons-react";
import "./StatisticsPage.css";

export function StatisticsPage() {
  const [summary, setSummary] = useState<StatisticsSummary | null>(null);
  const [recent, setRecent] = useState<UserAction[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [username, setUsername] = useState("");
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<UserAction | null>(null);

  useEffect(() => {
    loadData();
  }, [page]);

  const loadData = async () => {
    setLoading(true);
    try {
      const [summaryData, recentData] = await Promise.all([
        fetchStatisticsSummary(),
        fetchRecentActions(page, pageSize, username || undefined),
      ]);
      setSummary(summaryData);
      setRecent(recentData.items);
      setTotalCount(recentData.totalCount);
    } catch (error) {
      console.error("Error loading statistics:", error);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setPage(1);
    await loadData();
  };

  const getServiceColor = (service: string) => {
    const colors: { [key: string]: string } = {
      Reservation: "blue",
      Payment: "green",
      Loyalty: "orange",
    };
    return colors[service] || "gray";
  };

  const getStatusColor = (status: string) => {
    const colors: { [key: string]: string } = {
      Success: "green",
      Failed: "red",
      NoContent: "orange",
      NotFound: "orange",
    };
    return colors[status] || "gray";
  };

  const formatDate = (ts?: string) => {
    if (!ts) return "—";
    return new Date(ts).toLocaleString("ru-RU", {
      timeZone: "Europe/Moscow",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    });
  };

  return (
    <div className="statistics-page">
      <Container size="lg">
        <div className="statistics-header">
          <div className="title">
            Статистика системы
          </div>
          <Text size="m" className="statistics-subtitle">
            Мониторинг действий пользователей в реальном времени
          </Text>
        </div>

        {summary && (
          <>
            <SimpleGrid cols={{ base: 1, sm: 2, md: 3 }} className="summary-grid">
              <Card className="summary-card">
                <div className="card-icon">
                  <IconActivity size={24} />
                </div>
                <Text size="xl" fw={700} className="summary-value">
                  {summary?.totalActions?.toLocaleString('ru-RU')}
                </Text>
                <Text size="sm" c="dimmed">
                  Всего действий
                </Text>
              </Card>
              
              <Card className="summary-card">
                <div className="card-icon">
                  <IconUser size={24} />
                </div>
                <Text size="xl" fw={700} className="summary-value">
                  {summary?.activeUsers?.toLocaleString('ru-RU')}
                </Text>
                <Text size="sm" c="dimmed">
                  Активных (15 мин)
                </Text>
              </Card>

              <Card className="summary-card">
                <div className="card-icon">
                  <IconTrendingUp size={24} />
                </div>
                <Text size="xl" fw={700} className="summary-value">
                  {Object.keys(summary.actionsByType || {}).length}
                </Text>
                <Text size="sm" c="dimmed">
                  Типов действий
                </Text>
              </Card>
            </SimpleGrid>

            <SimpleGrid cols={{ base: 1, md: 3 }} className="analytics-grid">
              <Card className="analytics-card">
                <Text size="lg" fw={600} className="section-title">
                  Самые частые действия
                </Text>
                <div className="compact-table">
                  {Object.entries(summary.actionsByType || {})
                    .sort(([,a], [,b]) => b - a)
                    .slice(0, 5)
                    .map(([action, count]) => (
                      <div key={action} className="table-row-compact">
                        <Text size="sm" className="action-name">
                          {action}
                        </Text>
                        <Badge color="blue" variant="light" size="lg">
                          {count}
                        </Badge>
                      </div>
                    ))}
                </div>
              </Card>

              <Card className="analytics-card">
                <Text size="lg" fw={600} className="section-title">
                  Топ пользователей
                </Text>
                <div className="compact-table">
                  {(summary.topUsers || []).slice(0, 5).map((user, index) => (
                    <div key={user.username} className="table-row-compact">
                      <Group gap="xs">
                        <Text size="sm" fw={500} className="username">
                          {user.username}
                        </Text>
                        {index < 3 && (
                          <IconCrown 
                            size={16} 
                            color={index === 0 ? "#FFD700" : index === 1 ? "#C0C0C0" : "#CD7F32"} 
                          />
                        )}
                      </Group>
                      <Badge 
                        color={index === 0 ? "yellow" : index === 1 ? "gray" : "orange"} 
                        variant="light"
                        size="lg"
                      >
                        {user.count}
                      </Badge>
                    </div>
                  ))}
                </div>
              </Card>

              <Card className="analytics-card">
                <Text size="lg" fw={600} className="section-title">
                  Активность по дням
                </Text>
                <div className="compact-table">
                  {Object.entries(summary.actionsByDay || {})
                    .sort(([a], [b]) => new Date(b).getTime() - new Date(a).getTime())
                    .slice(0, 5)
                    .map(([date, count]) => (
                      <div key={date} className="table-row-compact">
                        <Text size="sm" c="dimmed" className="day-date">
                          {new Date(date).toLocaleDateString('ru-RU', {
                            day: 'numeric',
                            month: 'short'
                          })}
                        </Text>
                        <Badge color="green" variant={count > 30 ? "filled" : "light"} size="lg">
                          {count}
                        </Badge>
                      </div>
                    ))}
                </div>
              </Card>
            </SimpleGrid>
          </>
        )}

        <Card className="actions-card">
          <div className="card-section">
            <div className="section-header">
              <IconHistory size={24} />
              <Text size="xl" fw={600}>История действий</Text>
            </div>

            <form onSubmit={handleSearch} className="search-form">
              <Group className="search-group">
                <TextInput
                  placeholder="Поиск по username..."
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="search-input"
                  size="md"
                />
                <button
                  type="submit"
                  className="search-button"
                >
                  <span>Найти</span>
                </button>
              </Group>
            </form>

            <div className="table-container">
              <table className="actions-table">
                <thead>
                  <tr>
                    <th>Пользователь</th>
                    <th>Сервис</th>
                    <th>Действие</th>
                    <th>Статус</th>
                    <th>Время (MSK)</th>
                  </tr>
                </thead>
                <tbody>
                  {recent.map((action) => {
                    const mskDate = action.timestamp
                      ? formatDate(action.timestamp)
                      : "—";

                    return (
                      <tr
                        key={action.id}
                        className="table-row"
                        onClick={() => setSelected(action)}
                      >
                        <td className="username-cell">
                          <Text fw={500}>{action.username}</Text>
                        </td>
                        <td>
                          <Badge color={getServiceColor(action.service)} variant="light">
                            {action.service}
                          </Badge>
                        </td>
                        <td className="action-cell">
                          <Text>{action.action}</Text>
                        </td>
                        <td>
                          <Badge color={getStatusColor(action.status)}>
                            {action.status}
                          </Badge>
                        </td>
                        <td className="time-cell">
                          <Text size="sm" c="dimmed">
                            {mskDate}
                          </Text>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            {totalCount > pageSize && (
              <Pagination
                value={page}
                onChange={setPage}
                total={Math.ceil(totalCount / pageSize)}
                className="pagination"
              />
            )}

            {recent.length === 0 && !loading && (
              <div className="empty-state">
                <Text c="dimmed" size="lg">Действия не найдены</Text>
              </div>
            )}
          </div>
        </Card>
      </Container>

      <Modal
        opened={!!selected}
        onClose={() => setSelected(null)} 
        size="lg"
        title="Детали действия"
        className="details-modal"
      >
        {selected && (
          <Box className="modal-content">
            <CopyButton 
              value={JSON.stringify({
                ...selected,
                timestamp: selected.timestamp
                  ? formatDate(selected.timestamp)
                  : "—"
              }, null, 2)} 
              timeout={2000}
            >
              {({ copied, copy }) => (
                <Tooltip label={copied ? 'Скопировано!' : 'Скопировать'} withArrow position="right">
                  <ActionIcon
                    color={copied ? 'teal' : 'gray'}
                    onClick={copy}
                    className="copy-button"
                  >
                    {copied ? <IconCheck size={16} /> : <IconCopy size={16} />}
                  </ActionIcon>
                </Tooltip>
              )}
            </CopyButton>

            <pre className="json-pre">
              {JSON.stringify({
                ...selected,
                timestamp: selected.timestamp
                  ? formatDate(selected.timestamp)
                  : "—"
              }, null, 2)} 
            </pre>
          </Box>
        )}
      </Modal>
    </div>
  );
}

export default StatisticsPage;