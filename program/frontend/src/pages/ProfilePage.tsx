import { useEffect, useState } from "react";
import { getLoyalty, type LoyaltyInfoResponse } from "../api/LoyaltyClient";
import { Card, Container, Loader, Text, Group, Avatar, Skeleton, Progress } from "@mantine/core";
import { parseJwt } from "../utils/jwt";
import "./ProfilePage.css";
import { IconDiscount, IconCalendarStar, IconCrown, IconMedal, IconMedal2, IconUser, IconId } from "@tabler/icons-react";

export default function ProfilePage() {
  const [loyalty, setLoyalty] = useState<LoyaltyInfoResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [userInfo, setUserInfo] = useState<any | null>(null);

  useEffect(() => {
    const token = localStorage.getItem("access_token");
    if (token) {
      const payload = parseJwt(token);
      setUserInfo(payload);
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        const res = await getLoyalty();
        setLoyalty(res);
      } catch (err: any) {
        setError(err?.message ?? "Ошибка загрузки");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const translateStatus = (status: string) => {
    switch (status) {
      case "BRONZE": return { 
        text: "Бронзовый", 
        color: "#cd7f32", 
        icon: <IconMedal size={24} color="#cd7f32" />
      };
      case "SILVER": return { 
        text: "Серебряный", 
        color: "#c0c0c0", 
        icon: <IconMedal2 size={24} color="#c0c0c0" />
      };
      case "GOLD": return { 
        text: "Золотой", 
        color: "#ffd700", 
        icon: <IconCrown size={24} color="#ffd700" />
      };
      default: return { 
        text: status, 
        color: "#e0e0e0", 
        icon: <IconMedal size={24} />
      };
    }
  };

  const getNextLevelInfo = (reservationCount: number, currentStatus: string) => {
    if (currentStatus === "GOLD") {
      return { needed: 0, nextStatus: "MAX" };
    } else if (currentStatus === "SILVER") {
      return { needed: Math.max(0, 20 - reservationCount), nextStatus: "GOLD" };
    } else {
      return { needed: Math.max(0, 10 - reservationCount), nextStatus: "SILVER" };
    }
  };

  const getProgressPercentage = (reservationCount: number, currentStatus: string) => {
    if (currentStatus === "GOLD") return 100;
    if (currentStatus === "SILVER") return (reservationCount - 10) / 10 * 100;
    return reservationCount / 10 * 100;
  };

  return (
    <div className="profile-wrapper">
      <Container size="lg" className="profile-container">
        <Card shadow="lg" radius="lg" p={0} className="profile-main-card">
          <div className="profile-header-card">
            <div className="header-content">
              <IconUser size={32} color="white" />
              <h1 className="header-title">
                Профиль
              </h1>
              <p className="header-subtitle">
                Ваша личная информация и статус лояльности
              </p>
            </div>
          </div>

          <div className="profile-content-card">
            <div className="profile-grid">
              <Card shadow="sm" radius="md" p="lg" withBorder className="user-info-card">
                <div className="card-section">
                  <div className="section-header">
                    <IconId size={20} />
                    <Text size="sm" fw={600}>Личная информация</Text>
                  </div>
                  <Group align="flex-start" mt="md">
                    <Avatar size="lg" radius="xl" color="indigo" className="user-avatar">
                      {userInfo?.name?.[0] ?? "U"}
                    </Avatar>
                    <div className="user-info">
                      <Text size="md" fw={700} className="user-name">
                        {userInfo?.name}
                      </Text>
                    </div>
                  </Group>
                </div>

                <div className="info-grid">
                  <div className="info-item">
                    <Text size="xs" c="dimmed">Логин</Text>
                    <Text size="sm" fw={600} className="info-value">
                      {userInfo?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"]}
                    </Text>
                  </div>
                  <div className="info-item">
                    <Text size="xs" c="dimmed">Почта</Text>
                    <Text size="sm" fw={600} className="info-value">
                      {userInfo?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"]}
                    </Text>
                  </div>
                  <div className="info-item">
                    <Text size="xs" c="dimmed">Роль</Text>
                    <Text size="sm" fw={600} className="info-value">
                      {userInfo?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"]}
                    </Text>
                  </div>
                </div>
              </Card>

              {loading ? (
                <Skeleton height={200} radius="md" />
              ) : error ? (
                <Card radius="md" p="lg" bg="rgba(239, 68, 68, 0.1)" className="error-card">
                  <Text size="sm" color="red" ta="center">{error}</Text>
                </Card>
              ) : loyalty && (
                <Card shadow="sm" radius="md" p="lg" withBorder className="loyalty-card">
                  <div className="card-section">
                    <div className="section-header">
                      <IconMedal size={20} />
                      <Text size="sm" fw={600}>Программа лояльности</Text>
                    </div>
                    
                    <div className="loyalty-header">
                      {translateStatus(loyalty.status).icon}
                      <div>
                        <Text size="md" fw={700} className="status-text">
                          {translateStatus(loyalty.status).text}
                        </Text>
                        <Text size="xs" c="dimmed">Ваш статус</Text>
                      </div>
                    </div>
                  </div>

                  <Group justify="space-around" mt="md" mb="md">
                    <div className="stat-item">
                      <IconDiscount size={20} className="stat-icon" />
                      <Text size="xs" fw={600}>Скидка</Text>
                      <Text size="lg" fw={700} className="stat-value">
                        {loyalty.discount}%
                      </Text>
                    </div>
                    
                    <div className="stat-item">
                      <IconCalendarStar size={20} className="stat-icon" />
                      <Text size="xs" fw={600}>Бронирования</Text>
                      <Text size="lg" fw={700} className="stat-value">
                        {loyalty.reservationCount}
                      </Text>
                    </div>
                  </Group>

                  <div className="progress-section">
                    <Text size="xs" fw={600} mb="xs">
                      До {getNextLevelInfo(loyalty.reservationCount, loyalty.status).nextStatus === "MAX" 
                        ? "максимального уровня" 
                        : getNextLevelInfo(loyalty.reservationCount, loyalty.status).nextStatus === "SILVER" 
                        ? "Серебряного" 
                        : "Золотого"}
                    </Text>
                    <Progress 
                      value={getProgressPercentage(loyalty.reservationCount, loyalty.status)} 
                      color={translateStatus(loyalty.status).color}
                      size="md"
                      radius="xl"
                      className="progress-bar"
                    />
                    <Text size="xs" c="dimmed" mt="xs" ta="center">
                      {getNextLevelInfo(loyalty.reservationCount, loyalty.status).needed > 0 
                        ? `Осталось бронирований: ${getNextLevelInfo(loyalty.reservationCount, loyalty.status).needed}`
                        : "Достигнут максимальный уровень"}
                    </Text>
                  </div>
                </Card>
              )}
            </div>
          </div>
        </Card>
      </Container>
    </div>
  );
}