import { useEffect, useState } from "react";
import { getReservations, unbookHotel, type AggregatedReservationResponse } from "../api/ReservationsClient";
import { Badge, Button, Card, Container, Group, Loader, Text, Title } from "@mantine/core";
import { IconCalendar, IconClock, IconCheck, IconX, IconCurrentLocation } from "@tabler/icons-react";
import "./ReservationsPage.css";

export default function ReservationsPage() {
  const [reservations, setReservations] = useState<AggregatedReservationResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        setLoading(true);
        const res = (await getReservations()).reverse();
        setReservations(res);
      } catch (err: any) {
        setError(err?.message ?? "Ошибка загрузки");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const [isLoading, setIsLoading] = useState(false);
  const handleCancel = async (uid: string) => {
    try {
      setIsLoading(true);
      await unbookHotel(uid);
      setReservations((prev) => prev.filter((r) => r.reservationUid !== uid));
      const res = (await getReservations()).reverse();
      setReservations(res);
    } catch (err: any) {
      alert(err?.message ?? "Ошибка при отмене");
    } finally {
      setIsLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    const colors: { [key: string]: string } = {
      'PAID': 'green',
      'CANCELED': 'red',
    };
    return colors[status] || 'gray';
  };

  const formatDate = (dateString: string | number | Date) => {
    return new Date(dateString).toLocaleDateString('ru-RU', {
      day: 'numeric',
      month: 'short',
      year: 'numeric'
    });
  };

  const getReservationStatusByDate = (startDate: string, endDate: string, status: string) => {
  if (status === 'CANCELED') return 'CANCELED';

  const now = new Date();
  const start = new Date(startDate);
  const end = new Date(endDate);
  
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const startDay = new Date(start.getFullYear(), start.getMonth(), start.getDate());
  const endDay = new Date(end.getFullYear(), end.getMonth(), end.getDate());

  if (today > endDay) return 'COMPLETED';
  if (today >= startDay && today <= endDay) return 'ACTIVE';
  if (startDay.getTime() - today.getTime() <= 3 * 24 * 60 * 60 * 1000) return 'UPCOMING';
  return 'FUTURE';
};

  const canCancelReservation = (startDate: string, status: string) => {
    if (status === 'CANCELED') return false;
    const now = new Date();
    const start = new Date(startDate);
    return now < start;
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'ACTIVE': return <IconCurrentLocation size={16} />;
      case 'UPCOMING': return <IconClock size={16} />;
      case 'COMPLETED': return <IconCheck size={16} />;
      case 'CANCELED': return <IconX size={16} />;
      default: return <IconCalendar size={16} />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case 'ACTIVE': return 'Сейчас в отеле';
      case 'UPCOMING': return 'Скоро';
      case 'COMPLETED': return 'Завершено';
      case 'CANCELED': return 'Отменено';
      default: return 'Запланировано';
    }
  };

  const getStatusBannerColor = (status: string) => {
    switch (status) {
      case 'ACTIVE': return '#10b981';
      case 'UPCOMING': return '#3b82f6';
      case 'COMPLETED': return '#64748b';
      case 'CANCELED': return '#e53e3e';
      default: return '#667eea';
    }
  };

  if (loading) {
    return (
      <div className="reservations-page">
        <Container>
          <div className="reservations-loading">
            <Loader size="xl" />
            <Text mt="md">Загрузка бронирований...</Text>
          </div>
        </Container>
      </div>
    );
  }

  if (error) {
    return (
      <div className="reservations-page">
        <Container>
          <div className="reservations-error">
            <Text color="red">{error}</Text>
          </div>
        </Container>
      </div>
    );
  }

  return (
    <div className="reservations-page">
      <Container>
        <div className="reservations-header">
          <div className="title">
            Бронирования
          </div>
          <Text size="m" className="reservations-subtitle">
            Все ваши текущие и завершенные бронирования
          </Text>
        </div>

        {reservations.length === 0 ? (
          <div className="reservations-empty">
            <IconCalendar size={48} className="empty-icon" />
            <Text size="xl" fw={500}>Бронирования не найдены</Text>
            <Text c="dimmed">У вас пока нет активных бронирований</Text>
          </div>
        ) : (
          <div className="reservations-list">
            {reservations.map((r) => {
              const dateStatus = getReservationStatusByDate(r.startDate, r.endDate, r.status);
              const cardClass = `reservation-card ${dateStatus.toLowerCase()}-card`;
              const showCancelButton = canCancelReservation(r.startDate, r.status);
              const bannerColor = getStatusBannerColor(dateStatus);
              
              return (
                <Card key={r.reservationUid} radius="lg" p="xl" withBorder className={cardClass}>
                  <div className="status-banner" style={{ backgroundColor: bannerColor }}>
                    <Group gap="xs">
                      {getStatusIcon(dateStatus)}
                      <Text size="sm" fw={600} c="white">
                        {getStatusText(dateStatus)}
                      </Text>
                    </Group>
                  </div>

                  <div className="card-header">
                    <div className="hotel-info">
                      <Text fw={600} size="xl" className="hotel-name">
                        {r.hotel?.name ?? "Отель"}
                      </Text>
                      <Text size="sm" c="dimmed" className="hotel-address">
                        {r.hotel?.fullAddress}
                      </Text>
                    </div>
                    <div className="hotel-rating">
                      <div className="rating-badge">
                        ⭐ {r.hotel?.stars ?? "-"}
                      </div>
                    </div>
                  </div>

                  <div className="dates-section">
                    <Group justify="space-between">
                      <div className="date-item">
                        <Text size="xs" c="dimmed">Заезд</Text>
                        <Text fw={500} size="sm">{formatDate(r.startDate)}</Text>
                      </div>
                      <div className="date-arrow">→</div>
                      <div className="date-item">
                        <Text size="xs" c="dimmed">Выезд</Text>
                        <Text fw={500} size="sm">{formatDate(r.endDate)}</Text>
                      </div>
                    </Group>
                  </div>

                  <Group justify="space-between" mt="lg">
                    <div className="price-section">
                      <Text size="xs" c="dimmed">Итоговая стоимость</Text>
                      <Group align="center" gap="xs">
                        <Text fw={700} size="xl" className="price">
                          {r.payment?.price} ₽
                        </Text>
                      </Group>
                    </div>

                    <div className="status-section">
                      <Badge 
                        color={getStatusColor(r.status)} 
                        variant="light" 
                        size="lg"
                      >
                        {r.status === 'PAID' ? 'Оплачено' : 'Отменено'}
                      </Badge>
                    </div>
                  </Group>

                  {showCancelButton && (
                    <div className="cancel-section">
                    <Button
                      color="red"
                      variant="outline"
                      onClick={() => handleCancel(r.reservationUid)}
                      className="cancel-btn"
                      size="sm"
                      fullWidth
                      loading={isLoading}
                      loaderProps={{ type: 'dots' }}
                    >
                      {isLoading ? 'Отмена...' : 'Отменить бронирование'}
                    </Button>
                  </div>
                  )}
                </Card>
              );
            })}
          </div>
        )}
      </Container>
    </div>
  );
}